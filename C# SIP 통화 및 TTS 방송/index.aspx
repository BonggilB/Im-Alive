<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="index.aspx.cs" Inherits="QR_APP_Mize.Ba.TTS_Test" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <div style="width:1200px; align-items:center">
        <form id="form1" runat="server">
            <div>
                <asp:HiddenField ID="Call_status" runat="server" Value="0" />
                <p>방송하고싶은 내용을 입력해주세요</p>
                <asp:TextBox ID="tts_txt" runat="server"></asp:TextBox>
                <asp:LinkButton ID="talk_btn" runat="server" onclick="talk_Click" BorderWidth="2px" BorderColor="LightGray">말하기</asp:LinkButton>
            </div>
        </form>
    </div>
</body>
</html>
