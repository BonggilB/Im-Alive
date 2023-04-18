<%@ Page Language="C#" MasterPageFile="~/erp_master.Master" AutoEventWireup="true" CodeBehind="Locker.aspx.cs" Inherits="Mize_Cloud.Locker.Locker" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<asp:Content ID="header" runat="server" ContentPlaceHolderID="head">
    <script src="http://dmaps.daum.net/map_js_init/postcode.v2.js"></script>
    <script src="https://code.jquery.com/jquery-3.2.1.min.js"></script>
    <script src="http://code.jquery.com/jquery-latest.min.js"></script>
    <script src="../Script/jquery.bpopup.min.js"></script>

    <script type="text/javascript">
        window.onload = () => {
            let IR센서상태 = document.querySelector("#mulgun");

            setInterval(function () {
                $.ajax({
                    type: "Post",
                    url: "/Locker/Locker.aspx/CheckIRSensor",
                    contentType: "application/json; charset=utf-8",
                    data: "{}",
                    dataType: "json",
                    success: function (data) {
                        if (data.d != "00") {
                            IR센서상태.innerHTML = "물건이 <b style='color:red'>있음</b>";
                        } else {
                            IR센서상태.innerHTML = "물건이 <b>없음</b>";
                        }
                    },
                    error: function (request, status, error) {
                        //alert("서버에 연결할수 없습니다.");
                        console.log(status + error);
                    }
                })
            }, 2000);
        }

    </script>

    <style>
        input[type="number"]::-webkit-outer-spin-button,
        input[type="number"]::-webkit-inner-spin-button {
            -webkit-appearance: none;
            -moz-appearance: none;
            appearance: none;
        }

        #dimm_div {
            display: none;
            width: 100vw;
            height: 100vh;
            background-color: #808080;
        }
    </style>

</asp:Content>


<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <asp:ScriptManager ID="sc" runat="server" />
    <asp:HiddenField ID="hdnServerIP" runat="server" />
    <asp:HiddenField ID="hdnDeviceIDIP" runat="server" />
    <asp:HiddenField ID="hdnFaceTemplate" runat="server" />
    <asp:HiddenField ID="hdnIRTemplate" runat="server" />
    <asp:HiddenField ID="hdnIRImage" runat="server" />

    <div id="dimm_div" class="dimm_div">
        <img class="dimm_loading" src="../Images/loading.gif" / >
    </div>

    <div runat="server" id="add_title">
        <div class="title">
            열려라참깨
        </div>
        <div class="descript">
            제발열려라
        </div>
    </div>
    <!--기본정보, 회사정보-->

    <div class="tbl_info">
        <div class="iris">
            <div class="iris_column">
                <div class="iris_button">
                    <asp:LinkButton ID="btnCheckLocker" runat="server" OnClick="btnCheckLocker_Click">
                        상태(디버깅에서 확인)
                    </asp:LinkButton>
                    <asp:LinkButton ID="btnOpenLocker" runat="server" OnClick="btnOpenLocker_Click1">
                        잠금해제
                    </asp:LinkButton>
                    <asp:DropDownList ID="ddlComport" runat="server">
                        <asp:ListItem Value="0" Selected="True">01번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="1">02번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="2">03번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="3">04번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="4">05번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="5">06번 사물함 열기</asp:ListItem>
                        <asp:ListItem Value="6">07번 사물함 열기</asp:ListItem>
                    </asp:DropDownList>
                    <asp:DropDownList ID="ddlBoard" runat="server">
                        <%--<asp:ListItem Value="F" Selected="True">모든 보드</asp:ListItem>--%>
                        <asp:ListItem Value="0" Selected="True">왼쪽에있는 파란보드</asp:ListItem>
                        <asp:ListItem Value="1">오른쪽에있는 파란보드</asp:ListItem>
                        <asp:ListItem Enabled="false" Value="F">하하</asp:ListItem>
                        <asp:ListItem Enabled="false" Value="F">호호</asp:ListItem>
                        <asp:ListItem Enabled="false" Value="F">즐거운</asp:ListItem>
                        <asp:ListItem Enabled="false" Value="F">스머프마을</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
        </div>
    </div>
    <div class="tbl_info">
        <div class="iris">
            <div class="iris_column">
                <div class="iris_button">
                    <p>IR 센서 상태</p>
                    <p id="mulgun" >물건이..</p>
                </div>
            </div>
        </div>
    </div>

    <div id="popup_user" style="width: 40%">
        <div style="height: auto; width: 100%">
            <img id="imgOriginal" runat="server" style="width: 100%" />
        </div>
    </div>
    <!--//기본정보-->
</asp:Content>
