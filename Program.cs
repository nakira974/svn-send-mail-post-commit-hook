// See https://aka.ms/new-console-template for more information

using SharpSvn;
using System;
using System.Collections.ObjectModel;
using System.DirectoryServices.AccountManagement;
using System.Net.Mail;
using System.Text;

namespace post_commit
{
    internal  class Program
    {
        private static readonly string FromAddress = "svn-groupe@yourcompany.fr";
        private static readonly string ToAddress = "svn-groupe@yourcompany.fr";
        private static readonly string Title = "New commit from ";
        private static readonly string Body = "Revision n°";
    
        static void Main(string[] args)
        {
            
            var userName = string.Empty;
            var userMail = string.Empty;
            var commitMessage = string.Empty;
            var changeTime = string.Empty;
            var repositoryUrl = string.Empty;
            MailMessage mail;
            
            try
            {
               

                long currentVersion = 0;
                var appDataLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var svnAppPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "Subversion\\auth\\svn.simple");
                var filesName = Directory.GetFiles(svnAppPath);
                var svnConfigurationFile = filesName.FirstOrDefault();

                

                using (SvnClient client = new SvnClient())
                {
                    SvnInfoEventArgs info;
                    repositoryUrl = client.GetInfo(SvnPathTarget.FromString(Directory.GetCurrentDirectory()), out info) ? info.Uri.ToString() : string.Empty;
                    client.Update((Directory.GetCurrentDirectory()));
                    currentVersion =  client.GetInfo(SvnPathTarget.FromString(Directory.GetCurrentDirectory()), out info) ? info.Revision : 0;
                    changeTime = client.GetInfo(SvnPathTarget.FromString(Directory.GetCurrentDirectory()), out info) ? info.LastChangeTime.ToString("dddd dd MMMM yyyy") : string.Empty;
                    
                    var task = Task.Run(() => UserPrincipal.Current.EmailAddress);
                    if (task.Wait(TimeSpan.FromSeconds(1)))
                        userMail = task.Result;
                    else
                        throw new TimeoutException(
                            "L'adresse mail de l'utilisateur Windows courant n'est pas disponible dans un délais acceptable");
                    
                }

                GetCommiterName(svnConfigurationFile!, out userName);

                var smtpClient = new SmtpClient();
                smtpClient.Host = "smtp.gueudet.fr";
                 GetMailMessage(userName: userName, changeTime: changeTime, userMail: userMail,
                    repositoryUrl: repositoryUrl, currentVersion: currentVersion, mail: out mail);
                
                    smtpClient.Send(mail);

                
                Console.WriteLine($"{userName} has send a post-commit notification to {ToAddress} for revision n°{currentVersion}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        private static void GetMailMessage(string userName, string userMail, string changeTime, string repositoryUrl, long currentVersion, out MailMessage mail)
        {
            mail = new MailMessage();
            mail.Subject = Title + userName;
            mail.From = new MailAddress(userMail);
            mail.To.Add(ToAddress);
            //mail.Bcc.Add("sebmiz80@gmail.com");
                
            mail.BodyEncoding = Encoding.Latin1;
            GetLogMessage(repositoryUrl, currentVersion, out var commitMessage);
            var body =
                "<html>"+
                "<head>"+
                "<style>table, th, td {border: 1px solid black; border-collapse: collapse;}</style>"+
                "</head>"+
                "<body>" +
                $"<h1>{Body + currentVersion}</h1>" +
                "<table>" +
                "<thead>" +
                "<tr>" +
                "<td>Heure des changements</td>" +
                "<td>Message</td>" +
                "</tr>" +
                "</thead>" +
                "<tbody>" +
                "<tr>" +
                @$"<td>{changeTime}</td>" +
                @$"<td>{commitMessage}</td>" +
                "</tr>" +
                "</tbody>" +
                "</table>" +
                "</body>"+
                "</html>";

            mail.IsBodyHtml = true;
            mail.Body = body;
        }
        private static void GetLogMessage(string uri, long revision, out string commitMessage)
        {
            commitMessage = string.Empty;

            using (SvnClient cl = new SvnClient())
            {
                SvnLogArgs la = new SvnLogArgs();
                Collection<SvnLogEventArgs> col;
                la.Start = revision;
                la.End = revision;
                bool gotLog = cl.GetLog(new Uri(uri), la, out col);

                if (gotLog)
                {
                    commitMessage = @$"<li><a href=""{uri}"">Listes des fichiers changés</a> :<ul>";
                    foreach (var svnLogEventArgs in col)
                    {
                        foreach (var svnChangedItem in svnLogEventArgs.ChangedPaths)
                        {
                            var color = string.Empty;
                            switch (svnChangedItem.Action)
                            {
                                case SvnChangeAction.None:
                                    break;
                                case SvnChangeAction.Add:
                                    color = "#18ba4b";
                                    break;
                                case SvnChangeAction.Delete:
                                    color = "#d1193e";
                                    break;
                                case SvnChangeAction.Replace:
                                    color = "#d1a319";
                                    break;
                                case SvnChangeAction.Modify:
                                    color = "#b022d4";
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            commitMessage +=
                                @$"<li style=""color:{color}"">ACTION:{svnChangedItem.Action.ToString()}, ITEM:{svnChangedItem.RepositoryPath}</li>";

                        }
                    }
                    commitMessage += "</ul></li>";
                }
            }
        }

        private static void GetCommiterName(string svnConfigurationFile, out string userName)
        {
            if (string.IsNullOrEmpty(svnConfigurationFile)) throw new NullReferenceException("SVN configuration file's path is empty");
            var stream = new StreamReader(svnConfigurationFile!);
            var lines = new Dictionary<int, string>();
            int counter = 0;  
            string? ln;
                    
            while ((ln = stream.ReadLine()) != null) {
                lines.Add(counter, ln);
                counter++;  
            }

            userName = lines[counter - 2];
            stream.Close();
        }
    }
}

