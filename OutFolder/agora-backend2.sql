
CREATE TABLE metadata
(
  timestamp   TIMESTAMP,
  project     VARCHAR2(500 CHAR),
  elapsed_s   NUMBER(10, 6)
)
/

CREATE TABLE t
(
  project        VARCHAR2(500 CHAR),
  language       VARCHAR2(500 CHAR),
  file_fullname  VARCHAR2(500 CHAR),
  file_dirname   VARCHAR2(500 CHAR),
  file_basename  VARCHAR2(500 CHAR),
  nblank         INTEGER,
  ncomment       INTEGER,
  ncode          INTEGER,
  nscaled        NUMBER(10, 6)
)
/

insert into metadata values(TO_TIMESTAMP('2022-10-25 16:14:06','yyyy-mm-dd hh24:mi:ss'), 'test', 1.071057);
insert into t  values('test', 'CSS', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\bootstrap.min.css', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'bootstrap.min.css', 0, 5, 1, 1.000000);
insert into t  values('test', 'Markdown', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme\jp_readme.md', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme', 'jp_readme.md', 24, 0, 35, 35.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\usersapicontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'usersapicontroller.cs', 3, 0, 36, 48.960000);
insert into t  values('test', 'XML', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\web.release.config', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'web.release.config', 4, 21, 17, 32.300000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users\details.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users', 'details.cshtml', 9, 0, 33, 66.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll\rsabll.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll', 'rsabll.cs', 21, 19, 120, 163.200000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start\bundleconfig.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start', 'bundleconfig.cs', 5, 3, 22, 29.920000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\apiv1controller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'apiv1controller.cs', 17, 2, 241, 327.760000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\userscontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers', 'userscontroller.cs', 75, 75, 665, 904.400000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start\webapiconfig.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start', 'webapiconfig.cs', 2, 4, 31, 42.160000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\qcdapicontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'qcdapicontroller.cs', 124, 76, 873, 1187.280000);
insert into t  values('test', 'ASP.NET', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\global.asax', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'global.asax', 0, 0, 1, 1.290000);
insert into t  values('test', 'JavaScript', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\bootstrap.min.js', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'bootstrap.min.js', 0, 5, 1, 1.480000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models\projectsyncsettings.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models', 'projectsyncsettings.cs', 19, 1, 178, 242.080000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models\warehouse.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models', 'warehouse.cs', 2, 0, 51, 69.360000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\projectscontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'projectscontroller.cs', 73, 48, 476, 647.360000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared\_layout.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared', '_layout.cshtml', 2, 10, 69, 138.000000);
insert into t  values('test', 'Visual Studio Solution', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\gitlabmanager.sln', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'gitlabmanager.sln', 1, 1, 28, 28.000000);
insert into t  values('test', 'Markdown', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme\en_readme.md', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme', 'en_readme.md', 24, 0, 35, 35.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\_viewstart.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views', '_viewstart.cshtml', 0, 0, 3, 6.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users\index.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users', 'index.cshtml', 6, 0, 52, 104.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\warehouse\sync.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\warehouse', 'sync.cshtml', 0, 5, 248, 496.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll\smtpclientbll.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll', 'smtpclientbll.cs', 4, 10, 51, 69.360000);
insert into t  values('test', 'XML', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\web.debug.config', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'web.debug.config', 4, 21, 5, 9.500000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\systemcontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'systemcontroller.cs', 8, 2, 37, 50.320000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared\_loginpartial.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared', '_loginpartial.cshtml', 7, 1, 26, 52.000000);
insert into t  values('test', 'JavaScript', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\jquery.min.js', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'jquery.min.js', 0, 1, 4, 5.920000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models\user.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models', 'user.cs', 6, 3, 45, 61.200000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start\filterconfig.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start', 'filterconfig.cs', 1, 0, 12, 16.320000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext\dbcon.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext', 'dbcon.cs', 1, 0, 9, 12.240000);
insert into t  values('test', 'XML', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\packages.config', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'packages.config', 0, 0, 44, 83.600000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home\index.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home', 'index.cshtml', 3, 1, 27, 54.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext\agoradbcontext.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext', 'agoradbcontext.cs', 8, 0, 15, 20.400000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\homecontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers', 'homecontroller.cs', 6, 0, 29, 39.440000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\properties\assemblyinfo.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\properties', 'assemblyinfo.cs', 4, 16, 15, 20.400000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\gitlabcodeanalysiscontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'gitlabcodeanalysiscontroller.cs', 87, 18, 666, 905.760000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start\routeconfig.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start', 'routeconfig.cs', 2, 0, 21, 28.560000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext\applicationdbcontext.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\datacontext', 'applicationdbcontext.cs', 2, 0, 22, 29.920000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\warehouse\index.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\warehouse', 'index.cshtml', 5, 6, 206, 412.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models\gitlabapi.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models', 'gitlabapi.cs', 2, 0, 94, 127.840000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\warehouseapicontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'warehouseapicontroller.cs', 112, 68, 883, 1200.880000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home\about.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home', 'about.cshtml', 1, 0, 6, 12.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users\create.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users', 'create.cshtml', 11, 0, 53, 106.000000);
insert into t  values('test', 'MSBuild script', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\gitlabmanager.csproj', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'gitlabmanager.csproj', 0, 6, 312, 592.800000);
insert into t  values('test', 'SVG', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\fonts\glyphicons-halflings-regular.svg', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\fonts', 'glyphicons-halflings-regular.svg', 0, 0, 288, 288.000000);
insert into t  values('test', 'CSS', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\fakeloader.css', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'fakeloader.css', 10, 0, 77, 77.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\jobcontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers', 'jobcontroller.cs', 8, 6, 65, 88.400000);
insert into t  values('test', 'CSS', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\site.css', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'site.css', 24, 5, 120, 120.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\accountcontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers', 'accountcontroller.cs', 18, 2, 163, 221.680000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home\unauthorized.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\home', 'unauthorized.cshtml', 3, 0, 5, 10.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared\error.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\shared', 'error.cshtml', 2, 0, 12, 24.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll\dingtalkclientbll.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\bll', 'dingtalkclientbll.cs', 5, 0, 69, 93.840000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users\edit.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users', 'edit.cshtml', 12, 0, 58, 116.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\attribute\gitauthorizeattribute.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\attribute', 'gitauthorizeattribute.cs', 1, 0, 27, 36.720000);
insert into t  values('test', 'XML', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\web.config', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views', 'web.config', 5, 0, 38, 72.200000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models\entities.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\models', 'entities.cs', 4, 0, 24, 32.640000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\filters\checkloginfilter.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\filters', 'checkloginfilter.cs', 2, 1, 28, 38.080000);
insert into t  values('test', 'JavaScript', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\scripts\modernizr-2.8.3.js', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\scripts', 'modernizr-2.8.3.js', 287, 510, 609, 901.320000);
insert into t  values('test', 'JavaScript', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\scripts\fakeloader.min.js', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\scripts', 'fakeloader.min.js', 0, 0, 1, 1.480000);
insert into t  values('test', 'CSS', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\bootstrap-icons.css', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content', 'bootstrap-icons.css', 2, 0, 1388, 1388.000000);
insert into t  values('test', 'CSS', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\datatables\jquery.datatables.css', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\datatables', 'jquery.datatables.css', 3, 30, 415, 415.000000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\account\login.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\account', 'login.cshtml', 8, 4, 60, 120.000000);
insert into t  values('test', 'Markdown', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme\ch_readme.md', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\template\readme', 'ch_readme.md', 24, 0, 35, 35.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start\apiauthorizeattribute.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\app_start', 'apiauthorizeattribute.cs', 13, 10, 71, 96.560000);
insert into t  values('test', 'Razor', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users\delete.cshtml', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\views\users', 'delete.cshtml', 11, 0, 37, 74.000000);
insert into t  values('test', 'Markdown', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\readme.md', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'readme.md', 0, 0, 1, 1.000000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\global.asax.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'global.asax.cs', 3, 1, 26, 35.360000);
insert into t  values('test', 'JavaScript', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\datatables\jquery.datatables.js', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\content\datatables', 'jquery.datatables.js', 1985, 6998, 6371, 9429.080000);
insert into t  values('test', 'XML', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\web.config', 'c:\users\2200714\appdata\local\temp\rmix7cwwki', 'web.config', 6, 39, 147, 279.300000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api\accountapicontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\api', 'accountapicontroller.cs', 9, 1, 67, 91.120000);
insert into t  values('test', 'C#', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers\warehousecontroller.cs', 'c:\users\2200714\appdata\local\temp\rmix7cwwki\controllers', 'warehousecontroller.cs', 26, 56, 273, 371.280000);
