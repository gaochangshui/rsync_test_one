using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;
using System.IO;

namespace GitLabManager.Controllers
{
    public class  ClocAnalysisController : ApiController
    {
        [HttpGet]
        public void CodeRun()
        {
            var branch = "develop";
            var sourceFolder = @"C:\temp\agora-backend";
            var sqlFile = CodeAnalysi(sourceFolder, branch,"23123", "agora-backend");
        }

        private string CodeAnalysi(string sourceWork,string branchName,string ID,string ProjectName)
        {
            string exefile = AppDomain.CurrentDomain.BaseDirectory + "cloc.exe";
            string tempResposite = AppDomain.CurrentDomain.BaseDirectory + @"TempResposite\";

            var outDir = tempResposite + @"OutDir\";
            var tmpDir = tempResposite + @"TempDir\" + ID;
            var fileSQL = outDir + ProjectName + ".sql";

            Directory.CreateDirectory(tmpDir);
            Directory.CreateDirectory(outDir);

            var p = new Process();

            if (File.Exists(exefile))
            {
                try
                {
                    var param = branchName 
                        + " --quiet --git --sql-append" 
                        + " --sql-project=" + ID + "@@@@@" + ProjectName + "@@@@@" + branchName
                        + " --sdir=" + tmpDir
                        + " --sql="  + fileSQL;

                    var cmdLine = exefile + " " + param;

                    // 命令行参数设定
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardInput = true;

                    p.Start();
                    p.StandardInput.WriteLine("cd " + sourceWork);
                    p.StandardInput.WriteLine("cd c:");

                    p.StandardInput.WriteLine(cmdLine);
                    p.StandardInput.WriteLine("exit");

                    var outerr = p.StandardError.ReadToEnd();
                    if (outerr == "")
                    {
                        var output = p.StandardOutput.ReadToEnd();
                        return fileSQL;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
                finally
                {
                    Directory.Delete(tmpDir, true);
                    p.Close();
                } 
            }

            return null;
        }

        private bool DBInsert(string sqlFile)
        {
            
            return true;
        }
    }
}
