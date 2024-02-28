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
using System.Text.Json;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace sztu_xk
{
    public class Req : INotifyPropertyChanged
    {
        public string jx02id { get; set; }
        public string jx0404id { get; set; }
        private String _LessonName;
        public String LessonName {
            get { return _LessonName; }
            set {
                _LessonName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LessonName"));
            }
        }
        private int _TriedTimes;
        public int TriedTimes
        {
            get { return _TriedTimes; }
            set
            {
                _TriedTimes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TriedTimes"));
            }
        }
        private bool _isSucceed;
        public bool isSucceed
        {
            get { return _isSucceed; }
            set
            {
                _isSucceed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isSucceed"));
            }
        }
        private String _Message;
        public String Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Message"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public ObservableCollection<Req> Reqs { get; set; }
        public Dictionary<string, Req> Lessons { get; set; }
        public string XkId { get; set; }
        public string JSESSIONID1 { get; set; }
        public string JSESSIONID2 { get; set; }
        public string SERVERID { get; set; }
        public HomePage()
        {
            this.InitializeComponent();
            this.Reqs = new ObservableCollection<Req>();
            this.Lessons = new Dictionary<string, Req>();
            // ui of jwxt is not responsive
            this.webView.CoreWebView2Initialized += (s, a) =>
            {
                this.SizeChanged += (s, a) =>
                {
                    this.webView.Reload();
                };
            };
            this.webView.CoreWebView2Initialized += async (s, a) =>
            {
                // disable alert because it produce mis info
                await this.webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    window.alert = function () {}; 
                ");
                var manager = this.webView.CoreWebView2.CookieManager;
                var cookies = await manager.GetCookiesAsync(null);
                foreach (var cookie in cookies)
                {
                    System.Diagnostics.Debug.WriteLine(cookie.Name + ": " + cookie.Value + " at " + cookie.Path);
                    if (cookie.Name == "JSESSIONID" && cookie.IsHttpOnly && cookie.Path == "/")
                    {
                        JSESSIONID1 = cookie.Value;
                    }
                    if (cookie.Name == "JSESSIONID" && cookie.IsHttpOnly && cookie.Path != "/")
                    {
                        JSESSIONID2 = cookie.Value;
                    }
                    if (cookie.Name == "SERVERID")
                    {
                        SERVERID = cookie.Value;
                    }
                }
                this.webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                this.webView.CoreWebView2.WebResourceRequested += (s, args) =>
                {
                    args.Request.Headers.SetHeader("Referer", $"https://jwxt.sztu.edu.cn/jsxsd/xsxk/xsxk_index?jx0502zbid={this.XkId}");
                };
                this.webView.CoreWebView2.WebResourceResponseReceived += async (s, args) =>
                {
                    if (args.Request.Uri.Contains("https://jwxt.sztu.edu.cn/jsxsd/xsxkkc"))
                    {
                        // lesson list
                        if (args.Request.Method == "POST")
                        {
                            await args.Response.GetContentAsync().AsTask().ContinueWith(t =>
                            {
                                var content = new StreamReader(t.Result.AsStreamForRead()).ReadToEnd();
                                var json = JsonDocument.Parse(content);
                                var root = json.RootElement;
                                var lessonsList = root.GetProperty("aaData").EnumerateArray();
                                foreach (var lesson in lessonsList)
                                {
                                    var kcmc = lesson.GetProperty("kcmc").GetString();
                                    var fzmc = lesson.GetProperty("fzmc").GetString();
                                    var jx02id = lesson.GetProperty("jx02id").GetString();
                                    var jx0404id = lesson.GetProperty("jx0404id").GetString();
                                    this.Lessons[jx0404id] = new Req
                                    {
                                        jx02id = jx02id,
                                        jx0404id = jx0404id,
                                        LessonName = fzmc != null ? $"{kcmc}（{fzmc}）" : kcmc,
                                        TriedTimes = 0,
                                        isSucceed = false
                                    };
                                }
                            });
                        } else if (args.Request.Method == "GET")
                        {
                            System.Diagnostics.Debug.WriteLine(args.Request.Uri);
                            var url = args.Request.Uri;
                            var id_ind = url.IndexOf("&jx0404id=");
                            if (id_ind != -1)
                            {
                                var length = url.IndexOf('&', id_ind + 10) - id_ind - 10;
                                var req = Lessons[url.Substring(id_ind + 10, length)];
                                this.Reqs.Add(req);
                                this.dataGrid.ItemsSource = this.Reqs;
                                this.requestXK(url, req);
                            }
                        }
                    }
                };
            };
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string v && !string.IsNullOrWhiteSpace(v))
            {
                this.XkId = v;
            }
            base.OnNavigatedTo(e);
        }
        private async void requestXK(string url, Req req)
        {
            try
            {
                HttpClient client = new();
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"116\", \"Not)A;Brand\";v=\"24\", \"Microsoft Edge\";v=\"116\"");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("Cookie", $"JSESSIONID={JSESSIONID1}; JSESSIONID={JSESSIONID2}; SERVERID={SERVERID}");
                client.DefaultRequestHeaders.Add("Referer", $"https://jwxt.sztu.edu.cn/jsxsd/xsxkkc/comeInBxqjhxk");
                client.DefaultRequestHeaders.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                var res = await client.GetAsync(url);
                req.TriedTimes++;
                var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                var root = json.RootElement;
                var xkRes = root.GetProperty("message").GetString();
                req.Message = xkRes;
                if (xkRes.Contains("成功") || xkRes.Contains("当前教学班已选择"))
                {
                    req.isSucceed = true;
                    this.webView2.Reload();
                    return;
                }
                this.dataGrid.ItemsSource = this.Reqs;
                await Task.Delay(Settings.Default.INTERVAL);
                requestXK(url, req);
            } catch (Exception e)
            {
                req.Message = e.Message;
                this.dataGrid.ItemsSource = this.Reqs;
                await Task.Delay(Settings.Default.INTERVAL);
                requestXK(url, req);
            }
        }
    }
}
