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
[assembly: AssemblyVersion("1.1.8.16")]
[assembly: AssemblyFileVersion("1.0.0.0")]

/* v1.1.8.16 Editor of Luna plugin support snippets.
 * v1.1.8.15 Luna add function ScanQrcode().
 * v1.1.8.14 主窗口添加复制为vee订阅内容菜单项
 * v1.1.8.13 Fix bug in index refreshing.
 * v1.1.8.12 修复FormQRCode中链接和名称不对应的bug
 * v1.1.8.11 v:// ... 添加 crc8 校验位
 * v1.1.8.10 Enhance v:// ... share link format.
 *              ver 0a support vmess.alterId 
 *              ver 0b support shadowsocks protocol
 * v1.1.8.9 Refactor subscription.
 * v1.1.8.8 把 v:// ... 再缩短了几个字符
 * v1.1.8.7 用v2cfg:// ... 代替 v2ray:// ...
 * v1.1.8.6 v://... 支持quic
 * v1.1.8.5 修复v://...中的一些bugs
 * v1.1.8.4 支持v://...链接(自创的链接)
 * v1.1.8.3 添加ShareLinkMgr服务来处理各种分享链接
 * v1.1.8.2 Refactoring.
 * v1.1.8.1 尝试修复,GetFreeTcpPort()多线程下可能出错的问题
 */
