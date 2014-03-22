using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Xml;
using HtmlAgilityPack;

namespace NewsReader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {   
        /// <summary>
        /// この ID を ご自身の Yahoo! Japan のアプリケーション ID に書き換えてください。
        /// </summary>
        private const string APPLICATIOIN_ID = "dj0zaiZpPWd3MndNN2lIblJmTSZzPWNvbnN1bWVyc2VjcmV0Jng9ZDI-";

        private List<string> Categories = new List<string> { 
                                            "domestic",
                                            "world",
                                            "economy",
                                            "entertainment",
                                            "sports",
                                            "computer",
                                            "science",
                                            "local"
                                        };


        public MainWindow()
        {
            InitializeComponent();
        }




        private void ComboBoxCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ComboBoxCategories.SelectedIndex - 1;

            if (index >= 0)
            {
                StringBuilder url = new StringBuilder("http://news.yahooapis.jp/NewsWebService/V2/topics?");
                url.Append("appid=" + APPLICATIOIN_ID);
                url.Append("&pickupcategory=" + Categories[index]);

                string xml = string.Empty;

                try {
                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url.ToString());
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                    using (Stream stream = res.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        xml = reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == System.Net.WebExceptionStatus.ProtocolError)
                    {
                        HttpWebResponse errres = (HttpWebResponse)ex.Response;

                        if (errres.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MessageBox.Show("本日のリクエストの上限(5,000)に達しました。明日また試してください。", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                    }
                }


                //Delete News
                int count = ListBoxNews.Items.Count;

                for (int i = 0; i < count; i++)
                {
                    ListBoxNews.Items.Remove(ListBoxNews.Items[0]);
                }

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                XmlNameTable table = doc.NameTable;
                XmlNamespaceManager manager = new XmlNamespaceManager(table);
                manager.AddNamespace("ns", "urn:yahoo:jp:news");

                
                XmlNodeList list = doc.SelectNodes("/ns:ResultSet/ns:Result",manager);
                
                foreach (XmlNode node in list)
                {
                    XmlNode title = node.SelectSingleNode("ns:Title", manager);
                    XmlNode topiUrl = node.SelectSingleNode("ns:Url", manager);

                    TextBlock tb = new TextBlock();
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Height = 40; ;
                    tb.FontSize = 14;
                    tb.Text = title.InnerText;
                    tb.Tag = topiUrl.InnerText;
                    tb.Focusable = true;
                    tb.MouseDown += TextBlock_MouseDown;
                    tb.PreviewKeyDown += TextBlock_KeyDown;

                    ListBoxNews.Items.Add(tb);
                    

                }

                this.ScrollViewerListBox.ScrollToTop();
            }
        }


        private void TextBlock_MouseDown(object sender, MouseEventArgs e)
        {
            //e.Handled = true;
            TextBlock tb = (TextBlock)sender;
            GetHtml((string)tb.Tag);
            tb.Focus();
            

        }

        private void TextBlock_KeyDown(object sender, KeyboardEventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            GetHtml((string)tb.Tag);
            tb.Focusable = true;
            tb.Focus();
        }

        private void GetHtml(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                string textHtml;

                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("euc-jp")))
                {
                    textHtml = reader.ReadToEnd();
                }


                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(textHtml);

                foreach (HtmlNode h3 in html.DocumentNode.SelectNodes("//*[@id=\"detailHeadline\"]/h3"))
                {
                    //記事タイトル
                    this.TextBlockTitle.Text = h3.InnerText;

                    //記事本文
                    HtmlNode news = h3.NextSibling.NextSibling.NextSibling;

                    string newsDetail = news.InnerText;

                    if (newsDetail == null || newsDetail.Trim() == string.Empty)
                    {
                        this.TextBlockNewsDetail.Text = "Yahoo!ニュースのレイアウトが変わったため表示できません。";
                    }
                    else
                    {
                        this.TextBlockNewsDetail.Text = news.InnerText;
                    }
                }

                foreach (HtmlNode detail in html.DocumentNode.SelectNodes("//a[@class=\"readAll\"]"))
                {
                    string href = string.Empty;

                    var q = detail.Attributes.Where(a => a.Name == "href");

                    foreach (var item in q)
                    {
                        href = item.Value;
                    }

                    this.LabelNewsDetail.Tag = href;
                }
            }
            catch
            {
                //リトライ
            }
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label labelNewsDetail = (Label)sender;
            string href = (string)labelNewsDetail.Tag;

            if (href != null)
            {
                System.Diagnostics.Process.Start(href);
            }
        }





    }
}
