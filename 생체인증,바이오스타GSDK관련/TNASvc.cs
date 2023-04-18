using Tna;
using Grpc.Core;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System;

namespace MizeGSDK
{
    class TNASvc
    {
        private Tna.TNA.TNAClient tnaClient;

        public TNASvc(Channel channel)
        {
            tnaClient = new Tna.TNA.TNAClient(channel);
        }

        public TNAConfig GetConfig(uint deviceID)
        {
            var request = new Tna.GetConfigRequest { DeviceID = deviceID };
            var response = tnaClient.GetConfig(request);

            return response.Config;
        }

        public void SetConfig(uint deviceID, TNAConfig config)
        {
            var request = new Tna.SetConfigRequest { DeviceID = deviceID, Config = config };
            tnaClient.SetConfig(request);
        }

        public RepeatedField<TNALog> GetTNALog(uint deviceID, uint startEventID, uint maxNumOfLog)
        {
            var request = new GetTNALogRequest { DeviceID = deviceID, StartEventID = startEventID, MaxNumOfLog = maxNumOfLog };
            var response = tnaClient.GetTNALog(request);

            return response.TNAEvents;
        }
    }
}