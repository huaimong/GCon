using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("V2RayGCon")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("V2RayGCon")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("7b799000-e68f-450f-84af-5ec9a5eff384")]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
// 可以指定所有值，也可以使用以下所示的 "*" 预置版本号和修订号
// 方法是按如下所示使用“*”: :
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.7.7")]
[assembly: AssemblyFileVersion("1.0.0.0")]

/*
 * v1.1.7.7 重构CoreServerCtrl,分离出CoreInfo用于序列化
 * v1.1.7.6 出于安全考虑禁止从托盘菜单导入v2ray://...链接
 *          可从主窗口菜单中导入v2ray://...链接
 *          修复几个bugs
 * v1.1.7.5 issue #49 等待时间改成5秒  
 * v1.1.7.4 issue #49 解压前等待800ms,给系统释放资源
 * v1.1.7.3 issue #49 下载core窗口记住上次选择的core架构  
 * v1.1.7.2 修复更新core后,插件丢失的问题
 * v1.1.7.1 ICoreCtrl中添加几个函数，Luna中注入Each()函数
 */
