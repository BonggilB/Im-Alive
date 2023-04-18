using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using pqinet.comm;
using System.Net.Sockets;
using System.Threading;
using System.Web.Services;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.IO.Ports;
using System.IO;
using System.Linq;

namespace Mize_Cloud.Locker
{
    public partial class Locker : Page
    {
        #region 변수
        private static TcpClient client = null;
        #endregion

        #region 상수
        //BU 보드 IP 주소
        public string IPPort = "ㅋㅋ 트래픽이나 잡아먹자";
        public string Ipport = "107.254.37.255:5959";//존재하지 않는 IP와 포트입니다. 이러면 깃 가디언이 워닝 띄운다던데 궁금해서 일부러 한번 보ㄱ

        public bool IsPending;

        #endregion

        #region ASP
        protected void Page_Load(object sender, EventArgs e)
        {
            IsPending = false;
        }

        protected void btnOpenLocker_Click1(object sender, EventArgs e)
        {
            OpenLocker();
        }

        protected void btnCheckLocker_Click(object sender, EventArgs e)
        {
            CheckLocker();
        }

        protected void btnIRStatus_Click(object sender, EventArgs e)
        {
            CheckIRSensor();
        }
        #endregion
        public string OpenLocker()
        {
            string Result = string.Empty;

            try
            {
                //잠금장치포트
                byte byte_20 = (byte)int.Parse(this.ddlBoard.SelectedValue.ToString() + this.ddlComport.SelectedValue.ToString(), System.Globalization.NumberStyles.HexNumber);
                //잠금해제명령
                byte byte_30 = 0x31;

                Result = RequestBU(IPPort, byte_20, byte_30);
            }
            catch (Exception e)
            {
                //Exceptio문구는 기존 GSDK 사용자등록 가져와 쓴것이므로 신경X
                JScriptManager.Alert("서버에서 장치를 가져올수 없습니다.");
                client.Close();
            }
            return Result;
        }

        public string CheckLocker()
        {

            string Result = string.Empty;
            try
            {
                //모든락커
                byte byte_20 = 0xF0;
                //상태확인 요청
                byte byte_30 = 0x32;

                //응답데이터 디버깅으로 확인
                Result = RequestBU(IPPort, byte_20, byte_30);
            }
            catch (Exception e)
            {
                JScriptManager.Alert("서버에서 장치를 가져올수 없습니다.");
                //stream.Close();
                client.Close();
            }
            return null;
        }

        [WebMethod]
        public static string CheckIRSensor()
        {
            string Result = string.Empty;
            try
            {
                string IPPort = "107.254.37.255:5959";//존재하지 않는 IP와 포트입니다. 이러면 깃 가디언이 워닝 띄운다던데 궁금해서 일부러 한번 보ㄱ
                //IR센서주소
                byte byte_20 = 0x00;
                //상태확인 요청
                byte byte_30 = 0x30;

                //응답데이터 디버깅으로 확인
                string Response = RequestBU(IPPort, byte_20, byte_30);
                string [] ByteString = Response.Substring(Response.IndexOf("02-"),26).Split('-');

                // IR센서 상태여부가 현재 캐비닛엔 5번 배열임(보드종류에따라 변동가능)
                return ByteString[5];
            }
            catch (Exception e)
            {
                JScriptManager.Alert("서버에서 장치를 가져올수 없습니다.");
                //stream.Close();
                client.Close();
            }
            return null;
        }
        private static string RequestBU(string IPAddress, byte byte_second, byte byte_third)
        {
            string HasResult = "응애";

            client = new TcpClient();
            string[] IPPorts = IPAddress.Split(':');
            //시작 바이트
            byte byte_first = 0x02;
            //끝 바이트
            byte byte_fourth = 0x03;
            //오류검사
            byte LastSum = (byte)(byte_first + byte_second + byte_third + byte_fourth);

            byte[] bytes = new byte[] { byte_first, byte_second, byte_third, byte_fourth, LastSum };

            client.Connect(IPPorts[0], int.Parse(IPPorts[1]));

            if (client?.Connected == true)
            {
                NetworkStream stream = client.GetStream();

                stream.Write(bytes, 0, bytes.Length);

                //응답이 필요할경우
                if (byte_third == 0x32 || byte_third == 0x30)
                {
                    byte[] data = new byte[256];
                    HasResult = TCPreadHex(client, data);
                }
                else
                    HasResult = "무럭무럭요청완료";
                
                stream.Close();
                client.Close();
            }
            return HasResult;
        }
        private static string TCPreadHex(TcpClient client, byte[] data)
        {
            NetworkStream stream = client.GetStream();

            Int32 bytes = stream.Read(data, 0, data.Length);

            string hex = BitConverter.ToString(data);
            return hex/*.Replace("-", "")*/;
        }
        
        #region 언젠가 필요할것들
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private static void TCPwrite(TcpClient client, string msg)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);

            client.GetStream().Write(data, 0, data.Length);
        }
        private static void TCPHexwrite(TcpClient client, byte[] HexCode)
        {
            //byte[] data = new byte[] { 0x32, 0x16, 0x22 };

            client.GetStream().Write(HexCode, 0, HexCode.Length);
        }
        private static string TCPreadString(TcpClient client, byte[] data)
        {
            NetworkStream stream = client.GetStream();

            Int32 bytes = stream.Read(data, 0, data.Length);
            return Encoding.Default.GetString(data, 0, bytes);
        }
        #endregion
    }
}