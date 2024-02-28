using HtmlAgilityPack;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WebUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace sztu_xk
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WebviewPage : Page
    {
        public WebviewPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var manager = this.webview.CoreWebView2.CookieManager;
            var cookies = await manager.GetCookiesAsync(null);
            bool JSESSIONID1 = false;
            bool JSESSIONID2 = false;
            bool SERVERID = false;
            foreach (var cookie in cookies)
            {
                System.Diagnostics.Debug.WriteLine(cookie.Name + ": " + cookie.Value + " at " + cookie.Path);
                if (cookie.Name == "JSESSIONID" && cookie.IsHttpOnly && cookie.Path == "/")
                {
                    JSESSIONID1 = true;
                }
                if (cookie.Name == "JSESSIONID" && cookie.IsHttpOnly && cookie.Path != "/")
                {
                    JSESSIONID2 = true;
                }
                if (cookie.Name == "SERVERID")
                {
                    SERVERID = true;
                }
            }
            if (!(JSESSIONID1 && JSESSIONID2 && SERVERID))
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = this.XamlRoot,
                    Title = "无法找到Cookies",
                    PrimaryButtonText = "确定",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = "请确认已经成功登录，否则无法进行选课。"
                };

                await dialog.ShowAsync();
                return;
            }
            // fetch xk id
            this.webview.Source = new Uri("https://jwxt.sztu.edu.cn/jsxsd/xsxk/xklc_list");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.webview.CoreWebView2Initialized += delegate
            {
                // 禁用加载背景大图，加快加载速度
                this.webview.CoreWebView2.AddWebResourceRequestedFilter(
                    "https://auth.sztu.edu.cn/idp/themes/default/images/login_mainbg.jpg", CoreWebView2WebResourceContext.Image);
                this.webview.CoreWebView2.WebResourceRequested += (CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args) =>
                {
                    if (args.Request.Uri == "https://auth.sztu.edu.cn/idp/themes/default/images/login_mainbg.jpg")
                    {
                        CoreWebView2WebResourceResponse response = this.webview.CoreWebView2.Environment.CreateWebResourceResponse(
                            new MemoryStream(Encoding.UTF8.GetBytes("404")).AsRandomAccessStream(), 404, "Not Found", "Content-Type: text/plain");
                        args.Response = response;
                    }
                };
                this.webview.CoreWebView2.AddWebResourceRequestedFilter(
                    "https://jwxt.sztu.edu.cn/jsxsd/xsxk/xklc_list", CoreWebView2WebResourceContext.All);
                this.webview.CoreWebView2.WebResourceResponseReceived += async (CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args) =>
                {
                    if (args.Request.Uri == "https://jwxt.sztu.edu.cn/jsxsd/xsxk/xklc_list")
                    {
                        String XkId = "";
                        await args.Response.GetContentAsync().AsTask().ContinueWith((task) =>
                        {
                            var response = new StreamReader(task.Result.AsStreamForRead()).ReadToEnd();
                            try
                            {
                                var xk_ind = response.IndexOf("toxk");
                                if (xk_ind != -1)
                                {
                                    var length = response.IndexOf('\'', xk_ind + 6) - xk_ind - 6;
                                    XkId = response.Substring(xk_ind + 6, length);
                                }
                            } catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine(e);
                            }
                        });
                        if (XkId == "")
                        {
                            ContentDialog dialog = new()
                            {
                                XamlRoot = this.XamlRoot,
                                Title = "获取选课ID失败",
                                PrimaryButtonText = "确定",
                                DefaultButton = ContentDialogButton.Primary,
                                Content = "请确认选课是否开启。"
                            };

                            await dialog.ShowAsync();
                            return;
                        }
                        // notify server to enter xk
                        this.webview.Source = new Uri($"https://jwxt.sztu.edu.cn/jsxsd/xsxk/xsxk_index?jx0502zbid={XkId}");
                        this.webview.CoreWebView2.DOMContentLoaded += (CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args) =>
                        {
                            this.Frame.Navigate(typeof(HomePage), XkId);
                        };
                    }
                };
            };
        }
    }
}
