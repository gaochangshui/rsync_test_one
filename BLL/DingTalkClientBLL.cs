using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace DingDingManager.BLL
{
    public class DingTalkClientBLL
    {
        public static string URL = "https://oapi.dingtalk.com/topapi/message/corpconversation/";
        public static string SEND_URL = URL + "asyncsend_v2";
        public static string PROCESS_URL = URL + "getsendprogress";
        public static string RESULT_URL = URL + "getsendresult";
        public static string TOKEN_URL = "https://oapi.dingtalk.com/gettoken";

        public string GetToken()
        {
            IDingTalkClient client = new DefaultDingTalkClient(TOKEN_URL);
            OapiGettokenRequest req = new OapiGettokenRequest();
            req.Appkey = ConfigurationManager.AppSettings["Appkey"];
            req.Appsecret = ConfigurationManager.AppSettings["Appsecret"];
            req.SetHttpMethod("GET");
            OapiGettokenResponse rsp = client.Execute(req);
            Console.WriteLine(rsp.Body);
            return rsp.AccessToken;
        }
        public long SendMessage(string AccessToken, long AgentId, string DingID, string Content,string MessageUrl)
        {
            IDingTalkClient client = new DefaultDingTalkClient(SEND_URL);
            OapiMessageCorpconversationAsyncsendV2Request req = new OapiMessageCorpconversationAsyncsendV2Request();
            req.AgentId = AgentId;
            req.UseridList = DingID;// "user1,user2";
            OapiMessageCorpconversationAsyncsendV2Request.MsgDomain obj1 = new OapiMessageCorpconversationAsyncsendV2Request.MsgDomain();
            obj1.Msgtype = "text";
            OapiMessageCorpconversationAsyncsendV2Request.TextDomain obj2 = new OapiMessageCorpconversationAsyncsendV2Request.TextDomain();
            obj2.Content = Content;// "您的申请：url";
            obj1.Text = obj2;
            OapiMessageCorpconversationAsyncsendV2Request.LinkDomain obj3 = new OapiMessageCorpconversationAsyncsendV2Request.LinkDomain();
            obj3.MessageUrl = MessageUrl;
            obj1.Link = obj3;
            req.Msg_ = obj1;
            OapiMessageCorpconversationAsyncsendV2Response rsp = client.Execute(req, AccessToken);
            
            Console.WriteLine(rsp.Body);
            return rsp.TaskId;
        }

        public Boolean GetMessageProcess(string AccessToken, long AgentId, long TaskId)
        {
            IDingTalkClient client = new DefaultDingTalkClient(PROCESS_URL);
            OapiMessageCorpconversationGetsendprogressRequest req = new OapiMessageCorpconversationGetsendprogressRequest();
            req.AgentId = AgentId;
            req.TaskId = TaskId;
            OapiMessageCorpconversationGetsendprogressResponse rsp = client.Execute(req, AccessToken);
            Console.WriteLine(rsp.Body);
            return !rsp.IsError;
        }

        public Boolean GetMessageResult(string AccessToken, long AgentId, long TaskId)
        {
            IDingTalkClient client = new DefaultDingTalkClient(RESULT_URL);
            OapiMessageCorpconversationGetsendresultRequest req = new OapiMessageCorpconversationGetsendresultRequest();
            req.AgentId = AgentId;
            req.TaskId = TaskId;
            OapiMessageCorpconversationGetsendresultResponse rsp = client.Execute(req, AccessToken);
            Console.WriteLine(rsp.Body);
            return !rsp.IsError;
        }
    }
}