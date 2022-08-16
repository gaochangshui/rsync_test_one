using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace GitLabManager.BLL
{
    public class SmtpClientBLL
    {
        public static string emailAcount = ConfigurationManager.AppSettings["SmtpUser"];
        public static string emailPassword = ConfigurationManager.AppSettings["SmtpPassword"];
        public static string SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];

        public bool SendMail(string from,string to,string cc,string title,string body)
        {
            try
            {

                MailMessage message = new MailMessage();
                //设置发件人,发件人需要与设置的邮件发送服务器的邮箱一致
                MailAddress fromAddr = new MailAddress(from);
                message.From = fromAddr;
                //设置收件人,可添加多个,添加方法与下面的一样
                message.To.Add(to);
                //设置抄送人
                message.CC.Add(cc);
                //设置邮件标题
                message.Subject = title;
                //设置邮件内容
                message.Body = body;
                //html
                message.IsBodyHtml = true;
                //设置邮件发送服务器,服务器根据你使用的邮箱而不同,可以到相应的 邮箱管理后台查看,下面是QQ的
                SmtpClient client = new SmtpClient(SmtpServer, 25);
                //设置发送人的邮箱账号和密码
                client.Credentials = new NetworkCredential(emailAcount, emailPassword);
                //不启用ssl
                client.EnableSsl = false;
                //发送邮件
                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}