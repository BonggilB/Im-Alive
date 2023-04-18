using System;
using Server;
using Auth;
using User;
using Card;
using Finger;
using Google.Protobuf;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using Google.Protobuf.Collections;
using Event;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using Google.Protobuf.WellKnownTypes;
using System.Runtime.InteropServices;
using System.Net;

using MizeGSDK.Extensions;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Tna;
using Device;
using static Tna.TNA;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Data;
using static System.Net.WebRequestMethods;
using static Event.Event;
using System.Runtime.CompilerServices;
using Connect;
using System.Web.Services;
using Door;
using System.Threading.Tasks;

namespace MizeGSDK
{
    class MzAccess
    {
        #region 변수 및 상수
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private ServerSvc serverSvc;
        private AuthSvc authSvc;
        private CardSvc cardSvc;
        private TNASvc tnaSvc;
        private bool returnError;
        private MizeGSDK mizegsdk;

        HttpWebRequest httpWebRequest;

        const int C_SW_HIDE = 0; // 콘솔 숨기기
        const int C_SW_SHOW = 1; // 콘솔 보이기

        //상수
        private const int QUEUE_SIZE = 16;
        private const string TEST_USER_ID = "1";

        #endregion

        #region 개체

        //데몬에서 사용하는 Beans
        public class VerifyBeans
        {
            public ServerRequest ServerReq { get; set; } = null;
            public JObject Jobj { get; set; } = null;
            public string SiteName { get; set; } = null;
        }

        #endregion

        public MzAccess(ServerSvc serverSvc, AuthSvc authSvc, CardSvc cSvc, TNASvc tNASvc, MizeGSDK mizeGSDK)
        {
            this.serverSvc = serverSvc;
            this.authSvc = authSvc;
            this.cardSvc = cSvc;
            this.tnaSvc = tNASvc;
            this.mizegsdk = mizeGSDK;
            returnError = false;
        }

        public void ReadyToVerify(CancellationTokenSource Token)
        {
            try
            {
                serverSvc.Unsubscribe();
                var reqStream = serverSvc.Subscribe(QUEUE_SIZE);

                var DeviceInfos = mizegsdk.GetConnectSvc().GetDeviceList();

                ReadyServerMatching(reqStream, Token);
                //StartMonitoring(DeviceInfos, Token);

                KeyInput.PressEnter(">> 'ENTER'를 눌러 초기화면으로 돌아갑니다." + Environment.NewLine);

                //Console.WriteLine("2초뒤 백그라운드로 이동합니다.");

                //Thread.Sleep(2000);
                //IntPtr hWnd = GetConsoleWindow();
                //if (hWnd != IntPtr.Zero)
                //{
                //    ShowWindow(hWnd, C_SW_HIDE);
                //}

                Token.Cancel();
                serverSvc.Unsubscribe();
            }
            catch (RpcException e)
            {
                Console.WriteLine("오류!: 게이트웨이가 정상적으로 연결됐는지 확인 바랍니다.");
                throw;
            }
        }

        #region 서버매칭관련
        async void ReadyServerMatching(IAsyncStreamReader<ServerRequest> reqStream, CancellationTokenSource token)
        {
            Console.WriteLine("인증 요청받기 시작...");
            try
            {
                while (await reqStream.MoveNext(token.Token))
                {
                    try
                    {
                        var serverReq = reqStream.Current;

                        if (serverReq.ReqType == RequestType.VerifyRequest)
                            HandleVerify(serverReq);
                        else if (serverReq.ReqType == RequestType.IdentifyRequest)
                            HandleIdentify(serverReq);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
            }
            catch (RpcException e)
            {
                if (e.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("수신을 종료합니다");
                }
                else
                {
                    Console.WriteLine("오류: {0}", e);
                }
            }
        }

        private void HandleVerify(ServerRequest serverReq)
        {
            DeviceSvc deviceSvc = mizegsdk.GetDeviceSvc();

            CapabilityInfo capInfo = deviceSvc.GetCapabilityInfo(serverReq.DeviceID);

            Tna.Key GateDirection = Tna.Key.Unspecified;

            if (capInfo.Type == Device.Type.FacestationF2 || capInfo.Type == Device.Type.BioliteN2)
            {
                GateDirection = tnaSvc.GetConfig(serverReq.DeviceID).Key;
            }

            VerifyBeans VB = RequestVerifyIIS(serverReq, GateDirection.ToString());

            if (VB.Jobj.ContainsKey("Response"))
            {
                if (VB.Jobj["Response"].ToString() == "Open")
                {
                    Console.WriteLine("출입문개방 장치ID : {0}", serverReq.DeviceID);

                    var userInfo = new UserInfo { Hdr = new UserHdr { ID = TEST_USER_ID, NumOfCard = 1 } };
                    userInfo.Cards.Add(new CSNCardData { Data = VB.ServerReq.VerifyReq.CardData });
                    serverSvc.HandleVerify(VB.ServerReq, ServerErrorCode.Success, userInfo);
                }
                else
                {
                    Console.WriteLine("출입거부 장치ID : {0}", serverReq.DeviceID);

                    var userInfo = new UserInfo { Hdr = new UserHdr { ID = TEST_USER_ID, NumOfCard = 1 } };
                    serverSvc.HandleVerify(VB.ServerReq, ServerErrorCode.VerifyFail, null);
                }
            }
        }

        private void HandleIdentify(ServerRequest ServerReq)
        {
            try
            {
                FingerSvc finger = mizegsdk.GetFingerSvc();
                var FD_1 = new FingerData().Templates;// 지문 1
                FD_1.Add(ByteString.FromBase64("RSgQFKEAVUYpgLFHBTBCEEYGFILhsQkiQwGiCDPDQEIEIQPBpYs5BBCdhxzEYK0KK0TwoYczBPCdByPIEKaIFsghrwcUyHBUhzRJgaGDK4ngSIQSSvBViAdLEF0LOExAoIk6DFA/ihiM8E8GBw3AZg8JjhERixcOgEkRNU6wnYo8zvGUCS1PED6HBc8gcY87T7E2DxwP0D2NPxBgNQ8GkHBxlhpQsJsLEhExO94okXA2hxCRgC7JNVIhMYgn0kCPhT1SUIyOMJKRi4oqEuE1DP////8zP///////EjND//////EjNET/////ASI0RE////8BEjNERP///wESMzNE////ASIzMzT///8RIiMzNP///xESIzM0///+ARIiMzT//+4REiMzM///7hESIzMz///uESIjMzT//94BIjM0T/X8zRIjNERP9XrMI0RFVVZm+as0VVVVZmf5mWZmZmZmZ/iId3Zmd3Zn//h3d3d3ZVcAAAAAAAAAAAAAAAAAAAAA"));
                var FD_2 = new FingerData().Templates;// 지문 2
                FD_2.Add(ServerReq.IdentifyReq.TemplateData);

                FingerData CheckFinger = new FingerData();
                CheckFinger.Templates.Add(FD_1);
                CheckFinger.Templates.Add(FD_2);

                bool IsGo = finger.RequestVerify(ServerReq.DeviceID, CheckFinger);
            }
            catch
            {

            }
            try
            {
                try
                {
                    //DeviceSvc deviceSvc = mizegsdk.GetDeviceSvc();

                    //CapabilityInfo capInfo = deviceSvc.GetCapabilityInfo(serverReq.DeviceID);

                    //Tna.Key GateDirection = Tna.Key.Unspecified;

                    //if (capInfo.Type == Device.Type.FacestationF2 || capInfo.Type == Device.Type.BioliteN2)
                    //{
                    //    GateDirection = tnaSvc.GetConfig(serverReq.DeviceID).Key;
                    //}

                    //VerifyBeans VB = RequestVerifyIIS(serverReq, GateDirection.ToString());

                    //if (VB.Jobj.ContainsKey("Response"))
                    //{
                    //    if (VB.Jobj["Response"].ToString() == "Open")
                    //    {
                    //        Console.WriteLine("출입문개방 장치ID : {0}", serverReq.DeviceID);

                    //        var userInfo = new UserInfo { Hdr = new UserHdr { ID = TEST_USER_ID, NumOfCard = 1 } };
                    //        userInfo.Cards.Add(new CSNCardData { Data = VB.ServerReq.VerifyReq.CardData });
                    //        serverSvc.HandleVerify(VB.ServerReq, ServerErrorCode.Success, userInfo);
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("출입거부 장치ID : {0}", serverReq.DeviceID);

                    //        var userInfo = new UserInfo { Hdr = new UserHdr { ID = TEST_USER_ID, NumOfCard = 1 } };
                    //        serverSvc.HandleVerify(VB.ServerReq, ServerErrorCode.VerifyFail, null);
                    //        serverSvc.HandleVerify(VB.ServerReq, ServerErrorCode.VerifyFail, null);
                    //    }
                    //}
                }
                catch (Exception e)
                {

                }
            }
            catch (RpcException e)
            {
                if (e.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("수신을 종료합니다");
                }
                else
                {
                    Console.WriteLine("오류: {0}", e);
                }
            }
        }

        public VerifyBeans RequestVerifyIIS(ServerRequest serverReq, string DoorDirection)
        {
            VerifyBeans VB = new VerifyBeans { ServerReq = serverReq };
            JObject Jobject = new JObject();
            try
            {
                // QR값 변환
                string userData = string.Empty;
                var Datatype = VB.ServerReq.VerifyReq.CardData.GetType();
                if (Datatype.Name == "ByteString" && Datatype.IsSealed)
                {
                    userData = BitConverter.ToString(VB.ServerReq.VerifyReq.CardData.ToByteArray()).Replace("-", ""); // 카드데이터
                    userData = (Convert.ToInt64(userData.TrimStart('0'), 16)).ToString();  // 16진수 사용시
                }
                else
                {
                    userData = VB.ServerReq.VerifyReq.CardData.ToString();
                }

                VB = FindAddress(VB.ServerReq.DeviceID, VB);
                if (string.IsNullOrEmpty(VB.SiteName))
                {
                    Console.WriteLine("허가되지 않은 장치 : {0}", serverReq.DeviceID);
                    return VB;
                }

                //string IISAddress = new IniClass().ReadINI("Config", "IIS_Address") + FindAddress(serverReq.DeviceID);
                string IISAddress = "https://localhost:443/Sites/" + VB.SiteName + "/HandledVerify";

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
                httpWebRequest = (HttpWebRequest)WebRequest.Create(IISAddress);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";

                JObject Jobj = new JObject(
                    new JProperty("verifyJson",
                        new JObject(
                            new JProperty("DevID", VB.ServerReq.DeviceID),
                            new JProperty("CardNum", userData),
                            new JProperty("TNAKey", DoorDirection)
                        )
                    )
                );

                //IIS에서 JSON으로 받을수있는 방법 못찾아서 주석
                //JObject RequestJobj = new JObject();
                //RequestJobj.Add("Jobj",Jobj);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(Jobj.ToString());
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                try
                {
                    using (var response = httpWebRequest.GetResponse() as HttpWebResponse)
                    {
                        if (httpWebRequest.HaveResponse && response != null)
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                Jobject = JObject.Parse(reader.ReadToEnd());
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)e.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                string error = reader.ReadToEnd();
                                Jobject = JObject.Parse(error);
                            }
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                using (StreamReader streamReader = new StreamReader(webEx.Response.GetResponseStream(), true))
                {
                    string resultString = streamReader.ReadToEnd();

                    Jobject = JObject.Parse(resultString);
                }
            }

            VB.Jobj = JObject.Parse(Jobject["d"].ToString());

            return VB;
        }

        public VerifyBeans FindAddress(uint DevID, VerifyBeans VB)
        {
            //장치 ID별 IIS 주소찾기
            try
            {
                SqlConnection conn = new MizeUtils().GETSqlConnection();
                SqlCommand cmd = new SqlCommand("usp_get_device_address", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@devid", (int)DevID);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    VB.SiteName = dt.Rows[0]["address"].ToString();
                    VB.ServerReq.DeviceID = uint.Parse(dt.Rows[0]["dev_id"].ToString());
                }
            }
            catch (WebException webEx)
            {

            }

            return VB;
        }
        #endregion

        #region 이벤트관련
        public void StartMonitoring(RepeatedField<Connect.DeviceInfo> deviceInfos, CancellationTokenSource Token)
        {
            try
            {
                Event.Event.EventClient eventClient = mizegsdk.GetEventSvc().GetEvtClient();
                //uint[] deviceIDs = { };
                foreach (DeviceInfo deviceInfo in deviceInfos)
                {
                    try
                    {
                        //int i = deviceIDs.Length;
                        //Array.Resize(ref deviceIDs, i + 1);
                        //deviceIDs[i] = deviceInfo.DeviceID;
                        var enableRequest = new EnableMonitoringRequest { DeviceID = deviceInfo.DeviceID };
                        eventClient.EnableMonitoring(enableRequest);
                    }
                    catch
                    {
                        continue;
                    }
                }
                var subscribeRequest = new SubscribeRealtimeLogRequest { QueueSize = QUEUE_SIZE };
                var call = eventClient.SubscribeRealtimeLog(subscribeRequest);

                //cancellationTokenSource = new CancellationTokenSource();

                ReceiveEvents(mizegsdk.GetEventSvc(), call.ResponseStream, Token.Token);
            }
            catch (RpcException e)
            {
                Console.WriteLine("Cannot start monitoring: {0}", e);
                throw;
            }
        }

        public async void ReceiveEvents(EventSvc svc, IAsyncStreamReader<EventLog> stream, CancellationToken token)
        {
            Console.WriteLine("실시간 이벤트 감시 시작");

            try
            {
                while (await stream.MoveNext(token))
                {
                    EventLog eventLog = stream.Current;
                    var evtCallback = svc.GetEventCallback();

                    if (evtCallback != null)
                    {
                        evtCallback(eventLog);
                    }
                    else if (eventLog.EventCode == 4864)
                    {
                        Console.WriteLine("Event: {0}", eventLog);
                        EventIIS(eventLog);
                    }
                }
            }
            catch (RpcException e)
            {
                if (e.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("이벤트 모니터링 종료");
                }
                else
                {
                    Console.WriteLine("이벤트 모니터링 error: {0}", e);
                }
            }
        }

        public async void EventIIS(EventLog evtLog)
        {
            JObject Jobject = new JObject();
            try
            {
                //string IISAddress = new IniClass().ReadINI("Config", "IIS_Address") + FindAddress(serverReq.DeviceID);
                string IISAddress = "http://localhost:60504/Sites/TEST.asmx/HandleEvent";

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
                httpWebRequest = (HttpWebRequest)WebRequest.Create(IISAddress);
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";

                JObject Jobj = new JObject(
                    new JProperty("eventJson",
                        new JObject(
                            new JProperty("DevID", evtLog.DeviceID),
                            new JProperty("userID", evtLog.UserID),
                            new JProperty("timestamp", evtLog.Timestamp),
                            new JProperty("eventCode", evtLog.EventCode)
                        )
                    )
                );

                //IIS에서 JSON으로 받을수있는 방법 못찾아서 주석
                //JObject RequestJobj = new JObject();
                //RequestJobj.Add("Jobj",Jobj);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(Jobj.ToString());
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                try
                {
                    using (var response = httpWebRequest.GetResponse() as HttpWebResponse)
                    {
                        if (httpWebRequest.HaveResponse && response != null)
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                Jobject = JObject.Parse(reader.ReadToEnd());
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)e.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                string error = reader.ReadToEnd();
                                Jobject = JObject.Parse(error);
                            }
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                using (StreamReader streamReader = new StreamReader(webEx.Response.GetResponseStream(), true))
                {
                    string resultString = streamReader.ReadToEnd();

                    Jobject = JObject.Parse(resultString);
                }
            }
            var Job = JObject.Parse(Jobject["d"].ToString());

            if (Job.ContainsKey("Response"))
            {
                if (Job["Response"].ToString() == "Open")
                {
                    DoorSvc doorSvc = mizegsdk.GetDoorSvc();

                    var door = new DoorInfo
                    {
                        DoorID = 1,
                        Name = "Test Door",
                        EntryDeviceID = evtLog.DeviceID,
                        Relay = new Relay
                        {
                            DeviceID = evtLog.DeviceID,
                            Port = 0 // 1st relay
                        },
                        Sensor = new Sensor
                        {
                            DeviceID = evtLog.DeviceID,
                            Port = 0, // 1st input port
                            Type = SwitchType.NormallyOpen
                        },
                        Button = new ExitButton
                        {
                            DeviceID = evtLog.DeviceID,
                            Port = 1, // 2nd input port
                            Type = SwitchType.NormallyOpen
                        },
                        AutoLockTimeout = 3, // locked after 3 seconds
                        HeldOpenTimeout = 10 // held open alarm after 10 seconds
                    };

                    doorSvc.Add(evtLog.DeviceID, new DoorInfo[] { door });

                    var doorIDs = new uint[] { 1 };
                    doorSvc.Release(evtLog.DeviceID, doorIDs, DoorFlag.Operator);
                    Console.WriteLine("출입문이 열렸습니다 Door Status :Open");
                    doorSvc.Unlock(evtLog.DeviceID, doorIDs, DoorFlag.Operator);
                    await Task.Delay(3000).ContinueWith(_ =>
                    {
                        doorSvc.Lock(evtLog.DeviceID, doorIDs, DoorFlag.Operator);
                        Console.WriteLine("출입문이 닫혔습니다. Door Status :Closed");
                        doorSvc.DeleteAll(evtLog.DeviceID);
                    });
                }
            }
        }
        #endregion

        [WebMethod]
        public void WebRequsetUnlock(string DevID)
        {
            DoorSvc doorSvc = mizegsdk.GetDoorSvc();

            var doorIDs = new uint[] { 1 };
            doorSvc.Release(uint.Parse(DevID), doorIDs, DoorFlag.Operator);
            doorSvc.Unlock(uint.Parse(DevID), doorIDs, DoorFlag.Operator);
            Task.Delay(3000).ContinueWith(_ =>
            {
                doorSvc.Lock(uint.Parse(DevID), doorIDs, DoorFlag.Operator);
            });
        }
    }
}

