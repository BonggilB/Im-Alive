using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Event;
using Grpc.Core;
using Google.Protobuf.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using MizeGSDK.Extensions;
using Connect;
using System.Runtime.CompilerServices;

namespace MizeGSDK
{
    public delegate void EventCallback(EventLog logEvent);

    class EventSvc
    {
        private const int QUEUE_SIZE = 8;

        private Event.Event.EventClient eventClient;
        private CancellationTokenSource cancellationTokenSource;

        private EventCallback callback;
        private new MizeUtils.EventCodeMap codeMap;
        public EventCallback GetEventCallback()
        {
            return callback;
        }
        public Event.Event.EventClient GetEvtClient()
        {
            return this.eventClient;
        }

        public EventSvc(Channel channel)
        {
            eventClient = new Event.Event.EventClient(channel);
        }

        public void SetCallback(EventCallback eventCallback)
        {
            callback = eventCallback;
        }

        public RepeatedField<EventLog> GetLog(uint deviceID, uint startEventID, uint maxNumOfLog)
        {
            var request = new GetLogRequest { DeviceID = deviceID, StartEventID = startEventID, MaxNumOfLog = maxNumOfLog };
            var response = eventClient.GetLog(request);

            return response.Events;
        }

        public RepeatedField<EventLog> GetLogWithFilter(uint deviceID, uint startEventID, uint maxNumOfLog, EventFilter filter)
        {
            var request = new GetLogWithFilterRequest { DeviceID = deviceID, StartEventID = startEventID, MaxNumOfLog = maxNumOfLog };
            request.Filters.Add(filter);
            var response = eventClient.GetLogWithFilter(request);

            return response.Events;
        }

        public RepeatedField<ImageLog> GetImageLog(uint deviceID, uint startEventID, uint maxNumOfLog)
        {
            var request = new GetImageLogRequest { DeviceID = deviceID, StartEventID = startEventID, MaxNumOfLog = maxNumOfLog };
            var response = eventClient.GetImageLog(request);

            return response.ImageEvents;
        }

        public void EnableMonitoring(uint deviceID)
        {
            try
            {
                var enableRequest = new EnableMonitoringRequest { DeviceID = deviceID };
                eventClient.EnableMonitoring(enableRequest);
            }
            catch (RpcException e)
            {
                Console.WriteLine("Cannot enable monitoring {0}: {1}", deviceID, e);
                throw;
            }
        }

        //public void StartMonitoring(RepeatedField<Connect.DeviceInfo> deviceInfos, CancellationTokenSource Token)
        //{
        //    try
        //    {
        //        //uint[] deviceIDs = { }; Multi 한번에 넣으면 오류나서 홀라당 펑펑 날아가 주석
        //        foreach (DeviceInfo deviceInfo in deviceInfos)
        //        {
        //            try
        //            {
        //                //int i = deviceIDs.Length;
        //                //Array.Resize(ref deviceIDs, i + 1);
        //                //deviceIDs[i] = deviceInfo.DeviceID;
        //                var enableRequest = new EnableMonitoringRequest { DeviceID = deviceInfo.DeviceID };
        //                eventClient.EnableMonitoring(enableRequest);
        //            }
        //            catch
        //            {
        //                continue;
        //            }
        //        }
        //        var subscribeRequest = new SubscribeRealtimeLogRequest { QueueSize = QUEUE_SIZE };
        //        var call = eventClient.SubscribeRealtimeLog(subscribeRequest);

        //        //cancellationTokenSource = new CancellationTokenSource();

        //        ReceiveEvents(this, call.ResponseStream, Token.Token);
        //    }
        //    catch (RpcException e)
        //    {
        //        Console.WriteLine("Cannot start monitoring: {0}", e);
        //        throw;
        //    }
        //}

        //public void StartMonitoring(uint deviceID)
        //{
        //    try
        //    {
        //        var enableRequest = new EnableMonitoringRequest { DeviceID = deviceID };
        //        eventClient.EnableMonitoring(enableRequest);

        //        var subscribeRequest = new SubscribeRealtimeLogRequest { DeviceIDs = { deviceID }, QueueSize = QUEUE_SIZE };
        //        var call = eventClient.SubscribeRealtimeLog(subscribeRequest);

        //        cancellationTokenSource = new CancellationTokenSource();

        //        ReceiveEvents(this, call.ResponseStream, cancellationTokenSource.Token);
        //    }
        //    catch (RpcException e)
        //    {
        //        Console.WriteLine("Cannot start monitoring {0}: {1}", deviceID, e);
        //        throw;
        //    }
        //}
        //static async void ReceiveEvents(EventSvc svc, IAsyncStreamReader<EventLog> stream, CancellationToken token)
        //{
        //    Console.WriteLine("실시간 이벤트 감시 시작");
        //    try
        //    {
        //        while (await stream.MoveNext(token))
        //        {
        //            EventLog eventLog = stream.Current;
        //            var evtCallback = svc.GetEventCallback();

        //            if (evtCallback != null)
        //            {
        //                evtCallback(eventLog);
        //            }
        //            else if (eventLog.EventCode == 4864)
        //            {
        //                Console.WriteLine("Event: {0}", eventLog);
        //                FingerPrintTest(eventLog);
        //            }
        //        }
        //    }
        //    catch (RpcException e)
        //    {
        //        if (e.StatusCode == StatusCode.Cancelled)
        //        {
        //            Console.WriteLine("이벤트 모니터링 종료");
        //        }
        //        else
        //        {
        //            Console.WriteLine("이벤트 모니터링 error: {0}", e);
        //        }
        //    }
        //}

        public void StopMonitoring(uint deviceID)
        {
            var disableRequest = new DisableMonitoringRequest { DeviceID = deviceID };
            eventClient.DisableMonitoring(disableRequest);

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        public void DisableMonitoring(uint deviceID)
        {
            var disableRequest = new DisableMonitoringRequest { DeviceID = deviceID };
            eventClient.DisableMonitoring(disableRequest);
        }

        public void StopMonitoring()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        public void InitCodeMap(string filename)
        {
            var jsonData = File.ReadAllText(filename);
            codeMap = JsonSerializer.Deserialize<MizeUtils.EventCodeMap>(jsonData);
        }

        public string GetEventString(uint eventCode, uint subCode)
        {
            if (codeMap == null)
            {
                return string.Format("No code map(0x{0:X})", eventCode | subCode);
            }

            for (int i = 0; i < codeMap.entries.Count; i++)
            {
                if (eventCode == codeMap.entries[i].event_code && subCode == codeMap.entries[i].sub_code)
                {
                    return codeMap.entries[i].desc;
                }
            }

            return string.Format("Unknown event(0x{0:X})", eventCode | subCode);
        }
    }
}