using Auth;
using Grpc.Core;
using Google.Protobuf;
using System;

namespace MizeGSDK
{
  class AuthSvc
  {
    private Auth.Auth.AuthClient authClient;

    public AuthSvc(Channel channel) {
      authClient = new Auth.Auth.AuthClient(channel);
    }

    public AuthConfig GetConfig(uint deviceID) {
      var request = new Auth.GetConfigRequest{ DeviceID = deviceID };
      var response = authClient.GetConfig(request);

      return response.Config;
    }

    public void SetConfig(uint deviceID, AuthConfig config) {
      var request = new Auth.SetConfigRequest{ DeviceID = deviceID, Config = config };
      var response = authClient.SetConfig(request);
    }
  }
}