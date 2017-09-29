// Decompiled with JetBrains decompiler
// Type: EPiServer.Personalization.Internal.SubscriptionMail
// Assembly: EPiServer, Version=10.10.1.0, Culture=neutral, PublicKeyToken=8fe83dea738b45b7
// MVID: 1E5E6E8B-1F89-4CA6-B156-113C5229AAD8
// Assembly location: \packages\EPiServer.CMS.Core.10.10.1\lib\net45\EPiServer.dll

using EPiServer;
using EPiServer.Configuration;
using EPiServer.Core;
using EPiServer.Filters;
using EPiServer.Framework;
using EPiServer.Framework.Localization;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EPiServer.Personalization;

namespace Gosso.EpiserverLegacy.Personalization.Internal
{
    /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. The class that handles sending of mail notifications for subscriptions, used by <see cref="T:EPiServer.Personalization.SubscriptionJob" />.
    /// </summary>
    /// <remarks>
    /// <para>This subscription handler is going to deliver one subscription mail for each subscription root and language for the subcription, if it exists changed pages for it.</para>
    /// <para>The text for the "Read More..." text is taken from the page property "MailReadMore" on the subscription root.</para>
    /// <para>The subject of the mail is generated from the page property "MailSubject" on the subscription root.</para>
    /// <para>The mail from address generated from the "MailFrom" page property on the subscription root.</para>
    ///     The stylesheet is taken from the web.config
    ///     <code>
    ///     &lt;episerver xmlns="http://EPiServer.Configuration.EPiServerSection"&gt;
    ///       &lt;sites&gt;
    ///         &lt;site description="Example Site"&gt;
    ///           &lt;siteSettings
    ///             ...
    ///             uiEditorCssPaths="~/MyCss.css"
    ///     </code></remarks>
    /// <exclude />
    [SubscriptionPlugIn(DisplayName = "Default subscription handler", LanguagePath = "/admin/settings/defaultsubscriptionhandler")]
    //[Obsolete("The subscription feature was obsoleted in CMS 7.5 and is being phased out")]
    public class SubscriptionMail : ISubscriptionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SubscriptionMail));
        private readonly string _subscriptionMailDatePropertyName = "__SubscriptionMailDate";
        private string _userName;
        private EPiServerProfile _pers;
        private LocalizationService _localizationService;
        private ISiteDefinitionResolver _siteDefinitionResolver;

        private LocalizationService LocalizationService
        {
            get
            {
                return this._localizationService ?? LocalizationService.Current;
            }
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Personalized information
        /// </summary>
        /// <exclude />
        public EPiServerProfile UserData
        {
            get
            {
                return this._pers;
            }
            set
            {
                this._pers = value;
            }
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. SID
        /// </summary>
        /// <exclude />
        public string User
        {
            get
            {
                return this._userName;
            }
            set
            {
                this._userName = value;
            }
        }

        public SubscriptionMail()
            : this((LocalizationService)null, (ISiteDefinitionResolver)null)
        {
        }

        public SubscriptionMail(LocalizationService localizationService, ISiteDefinitionResolver siteDefinitionResolver)
        {
            this._localizationService = localizationService ?? LocalizationService.Current;
            this._siteDefinitionResolver = siteDefinitionResolver ?? ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>();
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Check if an email address conforms to the RFC 2822 email address protocol.
        /// For more information, see http://rfc.net/rfc2822.html.
        /// </summary>
        /// <param name="address">Email address to check</param>
        /// <returns>True if valid address, otherwise false</returns>
        /// <exclude />
        public virtual bool IsValidEmailAddress(string address)
        {
            if (address == null)
                return false;
            return Validator.EmailRegex.IsMatch(address);
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Send an e-mail to user with information about changed pages.
        /// </summary>
        /// <param name="subscriptionRootPage">The root page for the subscription that this mail should be based on</param>
        /// <param name="changedPages">The changed pages to notify the user about</param>
        /// <remarks>Create the subscription part of the body by calling the <see cref="M:EPiServer.Personalization.Internal.SubscriptionMail.GenerateBody(EPiServer.Core.PageData,EPiServer.Core.PageDataCollection)" /> function. </remarks>
        /// <exclude />
        public virtual void Send(PageData subscriptionRootPage, PageDataCollection changedPages)
        {
            string email = this.UserData.Email;
            if (!this.IsValidEmailAddress(email))
            {
                SubscriptionMail.log.Warn((object)("4.3.1  Invalid email address found (skip mail): " + (email == null ? "NULL" : email)));
            }
            else
            {
                MailMessage message = new MailMessage("subscription@" + SiteDefinition.Current.SiteUrl.Host, email);
                message.Headers.Add("X-Mailer", "EPiServer CMS");
                message.Headers.Add("Content-Base", SiteDefinition.Current.SiteUrl.GetLeftPart(UriPartial.Authority));
                message.Body = this.GenerateBody(subscriptionRootPage, changedPages);
                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;
                message.Subject = subscriptionRootPage["SubscriptionMailSubject"] == null ? this.LocalizationService.GetStringByCulture("/subscription/mailsubject", "Subscription mail", ((ILocalizable)subscriptionRootPage).Language) : subscriptionRootPage["SubscriptionMailSubject"] as string;
                if (subscriptionRootPage["SubscriptionMailFrom"] != null)
                    message.From = new MailAddress(subscriptionRootPage["SubscriptionMailFrom"] as string);
                new SmtpClient().Send(message);
                SubscriptionMail.log.Info((object)("4.3.2  Subscription mail sent to " + email));
            }
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Return the CSS data that is included in the subscription mail.
        /// By default this means fetching the contents of the file defined by the key
        /// EPsEditCSS in web.config. Any C style comments are automatically removed from the
        /// string before it is returned.
        /// </summary>
        /// <returns>A string with CSS data</returns>
        /// <remarks>Do not include any &lt;style&gt; tags in the string that is returned.</remarks>
        /// <exclude />
        public virtual string GetCSSContents()
        {
            string input = string.Empty;
            string[] strArray = Settings.Instance.UIEditorCssPaths.Split(',');
            if (strArray.Length == 0)
                return string.Empty;
            string virtualPath = UriSupport.ResolveUrlBySettings(strArray[0]);
            if (GenericHostingEnvironment.VirtualPathProvider.FileExists(virtualPath))
            {
                TextReader textReader = (TextReader)null;
                try
                {
                    textReader = (TextReader)new StreamReader(GenericHostingEnvironment.VirtualPathProvider.GetFile(virtualPath).Open());
                    input = textReader.ReadToEnd();
                }
                finally
                {
                    if (textReader != null)
                        textReader.Close();
                }
                SubscriptionMail.log.Debug((object)string.Format("4.3.3  Stylesheet used to format subscription mail: {0} ({1} bytes)", (object)virtualPath, (object)input.Length));
            }
            else
                SubscriptionMail.log.Warn((object)string.Format("4.3.4  Stylesheet not found: {0}", (object)virtualPath));
            return Regex.Replace(input, "/\\*.*(?>\\*/)", string.Empty).Trim();
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Create the Html for the subscription mail body.
        /// </summary>
        /// <param name="subscriptionPage">The subscription template page.</param>
        /// <param name="changedPagesReadOnly">A collection of readonly pages that have changed.</param>
        /// <returns>The complete Html for the mail body ("&lt;html&gt;.... &lt;/html&gt;").</returns>
        /// <remarks>
        /// 	Call the <see cref="M:EPiServer.Personalization.Internal.SubscriptionMail.GetCSSContents" /> function to fetch any CSS data that should be
        /// 	included in the html.
        /// </remarks>
        /// <exclude />
        public virtual string GenerateBody(PageData subscriptionPage, PageDataCollection changedPagesReadOnly)
        {
            string cssContents = this.GetCSSContents();
            PageDataCollection pages = new PageDataCollection();
            foreach (PageData pageData in changedPagesReadOnly)
            {
                PageData writableClone = pageData.CreateWritableClone();
                SubscriptionMail.log.Debug((object)("4.3.5 Adding page to subscription pageLink=" + (object)writableClone.PageLink));
                DateTime changed = writableClone.Changed;
                DateTime? nullable = writableClone.StartPublish;
                if ((nullable.HasValue ? (changed > nullable.GetValueOrDefault() ? 1 : 0) : 0) != 0)
                {
                    writableClone.Property.Add(this._subscriptionMailDatePropertyName, (PropertyData)new PropertyDate(writableClone.Changed));
                }
                else
                {
                    PropertyDataCollection property = writableClone.Property;
                    string datePropertyName = this._subscriptionMailDatePropertyName;
                    nullable = writableClone != null ? writableClone.StartPublish : new DateTime?();
                    PropertyDate propertyDate = new PropertyDate(nullable ?? DateTime.MaxValue);
                    property.Add(datePropertyName, (PropertyData)propertyDate);
                }
                pages.Add(writableClone);
            }
            new FilterPropertySort(this._subscriptionMailDatePropertyName, FilterSortDirection.Descending).Filter((object)this, new FilterEventArgs(pages));
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<html>");
            stringBuilder.Append("<head>");
            if (cssContents.Length > 0)
                stringBuilder.AppendFormat("<style>{0}</style>", (object)cssContents);
            stringBuilder.Append("</head>");
            stringBuilder.Append("<body>");
            if (subscriptionPage["SubscriptionMailBody"] != null)
                stringBuilder.Append(subscriptionPage["SubscriptionMailBody"]);
            foreach (PageData page in pages)
            {
                page.MakeReadOnly();
                stringBuilder.Append(this.FormatPageForBody(subscriptionPage, page));
            }
            stringBuilder.Append("</body></html>");
            return stringBuilder.ToString();
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Format info for a page that has changed and will be included inside the body of
        /// a subscription mail.
        /// </summary>
        /// <param name="subscriptionPage">The subscription page</param>
        /// <param name="page">Page that holds information that needs to be be formatted</param>
        /// <returns>A string that contains HTML by default</returns>
        /// <exclude />
        public virtual string FormatPageForBody(PageData subscriptionPage, PageData page)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<br><span class=MailDate>");
            stringBuilder.Append("[");
            stringBuilder.Append(((DateTime)page[this._subscriptionMailDatePropertyName]).ToShortDateString());
            stringBuilder.Append("]</span>");
            stringBuilder.Append("<br><span class=MailPageName>");
            stringBuilder.Append(page.Property["PageName"].ToWebString());
            stringBuilder.Append("</span><br><span class=MailPagePreview>");
            if (page["MainIntro"] != null)
            {
                stringBuilder.Append(page.Property["MainIntro"].ToWebString());
                stringBuilder.Append("<br>");
            }
            else
            {
                string str;
                if ((str = this.StripHtml(page["PageName"] as string, page["MainBody"] as string, (int)byte.MaxValue)) != null)
                {
                    stringBuilder.Append(str);
                    stringBuilder.Append("<br>");
                }
            }
            stringBuilder.Append("</span><a class=MailReadMore href=\"");
            if (page.LinkURL != null && page.LinkURL.StartsWith("/") && !page.LinkURL.StartsWith("//"))
            {
                SiteDefinition byContent = this._siteDefinitionResolver.GetByContent((ContentReference)page.PageLink, true, true);
                stringBuilder.Append(byContent.SiteUrl.GetLeftPart(UriPartial.Authority));
            }
            if (page.LinkType == PageShortcutType.External)
            {
                stringBuilder.Append(page.LinkURL);
            }
            else
            {
                UrlBuilder url = new UrlBuilder(UriSupport.AddLanguageSelection(page.LinkURL, ((ILocalizable)page).Language.TwoLetterISOLanguageName));
                Global.UrlRewriteProvider.ConvertToExternal(url, (object)page.PageLink, Encoding.UTF8);
                stringBuilder.Append(url.ToString());
            }
            stringBuilder.Append("\">");
            if (subscriptionPage["SubscriptionMailReadMore"] != null)
                stringBuilder.Append(subscriptionPage["SubscriptionMailReadMore"].ToString());
            else
                stringBuilder.Append(this.LocalizationService.GetStringByCulture("/subscription/pagelinktext", "Read more", ((ILocalizable)subscriptionPage).Language));
            stringBuilder.Append("</a>");
            stringBuilder.Append("<br>");
            return stringBuilder.ToString();
        }

        private string StripHtml(string title, string html, int len)
        {
            if (title == null || html == null)
                return (string)null;
            if (html.Length > len * 10)
                html = html.Substring(0, len * 10);
            html = html.Replace("<br>", " ");
            html = html.Replace("<BR>", " ");
            html = html.Replace("</p>", " ");
            html = html.Replace("</P>", " ");
            string str = HttpUtility.HtmlDecode(new Regex("<[^>]*>", RegexOptions.IgnorePatternWhitespace).Replace(html, "")).TrimStart(' ');
            if (str.StartsWith(title, StringComparison.OrdinalIgnoreCase))
                str = str.Remove(0, title.Length).TrimStart(' ');
            if (str.Length > len)
                str = str.Substring(0, len);
            int num = str.LastIndexOfAny(new char[2] { ' ', '.' });
            if (num > str.Length / 2)
                str = str.Substring(0, num + 1);
            if (str.Length > 0)
                return str + "..";
            return (string)null;
        }
    }
}
