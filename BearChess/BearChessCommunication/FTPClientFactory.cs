using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.BearChessCommunication
{
    public static class FtpClientFactory
    {
        public static IBearChessFtpClient GetFtpClient(ILogging logging)
        {
            var username = Configuration.Instance.GetConfigValue("publishUserName", string.Empty);
            var password = Configuration.Instance.GetSecureConfigValue("publishPassword", string.Empty);
            var server = Configuration.Instance.GetConfigValue("publishServer", string.Empty);
            var port = Configuration.Instance.GetConfigValue("publishPort", "21");
            var sftp = Configuration.Instance.GetBoolValue("publishSFTP",  false);
            return GetFtpClient(username, password, server, port, sftp, logging);
           
        }

        public static IBearChessFtpClient GetFtpClient(string username, string password, string server, string port, bool sftp, ILogging logging)
        {
           
            if (string.IsNullOrWhiteSpace(server) || server.StartsWith("file", StringComparison.OrdinalIgnoreCase))
            {
                return new BearChessTestFtpClient(server, username, password, logging);
            }
            if (sftp)
            {
                return new BearChessSFtpClient(server, username, password, port, logging);
            }

            return new BearChessFtpClient(server, username, password, port, logging);
        }
    }
}
