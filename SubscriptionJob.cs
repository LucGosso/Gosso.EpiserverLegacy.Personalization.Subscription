// Decompiled with JetBrains decompiler
// Type: EPiServer.Personalization.SubscriptionJob
// Assembly: EPiServer, Version=10.10.1.0, Culture=neutral, PublicKeyToken=8fe83dea738b45b7
// MVID: 1E5E6E8B-1F89-4CA6-B156-113C5229AAD8
// Assembly location: \packages\EPiServer.CMS.Core.10.10.1\lib\net45\EPiServer.dll

using EPiServer.Configuration;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess.Internal;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging.Compatibility;
using EPiServer.PlugIn;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Security.Principal;
using System.Threading;
using System.Web.Profile;
using EPiServer;
using EPiServer.Personalization;

namespace Gosso.EpiserverLegacy.Personalization
{
    /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Job that handles subscriptions, scheduled automatically by EPiServer Scheduler and should not be called in code.
    /// </summary>
    /// <internal-api />
    /// <exclude />
    [ScheduledPlugIn(DisplayName = "Subscription (Non Epi)", GUID = "60550A3B-09F2-4209-9760-77CCBBB60550", HelpFile = "subscriptionjob", LanguagePath = "/admin/databasejob/subscription")]
    //[Obsolete("The subscription feature was obsoleted in CMS 7.5 and is being phased out")]
    public class SubscriptionJob
    {
        private static object _syncObject = new object();
        private static readonly ILog log = LogManager.GetLogger(typeof(SubscriptionJob));
        private const int _maxPagesPerSubscription = 100;
        private const int _profilePageSize = 1000;
        private IContentRepository _contentRepository;
        private LocalizationService _localizationService;
        private readonly IUserImpersonation _userImpersonation;
        private ISiteDefinitionResolver _siteDefinitionResolver;
        private Gosso.EpiserverLegacy.DataAccess.Internal.SubscriptionDB _subscriptionDB;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Personalization.SubscriptionJob" /> class.
        /// </summary>
        public SubscriptionJob()
            : this((IContentRepository)null, (LocalizationService)null, (Gosso.EpiserverLegacy.DataAccess.Internal.SubscriptionDB)null, (ISiteDefinitionResolver)null, (ILanguageBranchRepository)null, (IUserImpersonation)null)
        {
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Initializes a new instance of the <see cref="T:EPiServer.Personalization.SubscriptionJob" /> class.
        /// </summary>
        /// <param name="contentRepository">
        /// </param>
        /// <param name="localizationService">
        /// </param>
        /// <param name="subscriptionDataAccess">
        /// </param>
        /// <param name="siteDefinitionResolver">
        /// </param>
        /// <exclude />
        [Obsolete("Use alternative constructor")]
        public SubscriptionJob(IContentRepository contentRepository, LocalizationService localizationService, Gosso.EpiserverLegacy.DataAccess.Internal.SubscriptionDB subscriptionDataAccess, SiteDefinitionResolver siteDefinitionResolver, IUserImpersonation userImpersonation)
            : this(contentRepository, localizationService, subscriptionDataAccess, (ISiteDefinitionResolver)siteDefinitionResolver, ServiceLocator.Current.GetInstance<ILanguageBranchRepository>(), userImpersonation)
        {
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Initializes a new instance of the <see cref="T:EPiServer.Personalization.SubscriptionJob" /> class.
        /// </summary>
        /// <exclude />
        public SubscriptionJob(IContentRepository contentRepository, LocalizationService localizationService, Gosso.EpiserverLegacy.DataAccess.Internal.SubscriptionDB subscriptionDataAccess, ISiteDefinitionResolver siteDefinitionResolver, ILanguageBranchRepository languageBranchRepository, IUserImpersonation userImpersonation)
        {
            this._contentRepository = contentRepository ?? (IContentRepository)DataFactory.Instance;
            this._localizationService = localizationService;
            this._userImpersonation = userImpersonation;
            this._subscriptionDB = subscriptionDataAccess ?? ServiceLocator.Current.GetInstance<Gosso.EpiserverLegacy.DataAccess.Internal.SubscriptionDB>();
            this._siteDefinitionResolver = siteDefinitionResolver ?? ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>();
            this._languageBranchRepository = languageBranchRepository ?? ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();
        }

        /// <summary>Service entrypoint</summary>
        public static string Execute()
        {
            bool lockTaken = false;
            Monitor.TryEnter(SubscriptionJob._syncObject, ref lockTaken);
            if (!lockTaken)
                return "Job was already running";
            try
            {
                return ServiceLocator.Current.GetInstance<SubscriptionJob>().InternalExecute();
            }
            finally
            {
                Monitor.Exit(SubscriptionJob._syncObject);
            }
        }

        /// <summary>Execute subscription job</summary>
        protected virtual string InternalExecute()
        {
            int num1 = 0;
            int num2 = 0;
            label_1:
            int totalRecords;
            ProfileInfoCollection allProfiles = ProfileManager.GetAllProfiles(ProfileAuthenticationOption.All, num2++, 1000, out totalRecords);
            if (allProfiles.Count == 0)
                return string.Format("{0} user profiles were found. {1} subscription e-mails were sent.", (object)totalRecords, (object)num1);
            IEnumerator enumerator = allProfiles.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    EPiServerProfile profile = EPiServerProfile.Get(((ProfileInfo)enumerator.Current).UserName);
                    if (!SubscriptionJob.IsInInterval(profile.SubscriptionInfo.Interval, profile.SubscriptionInfo.LastMessage))
                    {
                        int num3 = this.SendSubscriptions(profile);
                        if (num3 >= 0)
                        {
                            profile.SubscriptionInfo.LastMessage = DateTime.Now;
                            num1 += num3;
                            profile.Save();
                        }
                    }
                }
                goto label_1;
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        /// <summary>
        /// Process subscriptions and send notification about changes to subscribers.
        /// </summary>
        /// <param name="profile">The profile for the subscriber</param>
        /// <returns>
        /// Actual number of subscription notification messages sent.
        /// </returns>
        protected virtual int SendSubscriptions(EPiServerProfile profile)
        {
            int num = 0;
            IPrincipal principal = _userImpersonation.CreatePrincipal(profile.UserName);
            foreach (SubscriptionDescriptor subscribedPage in profile.SubscriptionInfo.SubscribedPages)
            {
                PageData pageData;
                try
                {
                    pageData = this._contentRepository.Get<PageData>((ContentReference)new PageReference(subscribedPage.PageID));
                }
                catch (ContentNotFoundException)
                {
                    if (SubscriptionJob.log.IsWarnEnabled)
                    {
                        SubscriptionJob.log.WarnFormat("The user {0} subscribes to the page {1} that does not exists.", (object)profile.UserName, (object)subscribedPage.PageID);
                        continue;
                    }
                    continue;
                }
                if (pageData != null && pageData["EPSUBSCRIBE"] != null)
                {
                    foreach (string language in subscribedPage.Languages)
                    {
                        PageDataCollection changedPages = this.GetChangedPages(profile, language, subscribedPage, principal);
                        if (changedPages.Count != 0)
                        {
                            PageData rootPage = this.GetPage(subscribedPage.PageID, language, principal, false) ?? SubscriptionJob.CreateLanguageBranchContainer(language);
                            if (this.CanSendSubscription(profile, rootPage, changedPages))
                            {
                                ++num;
                                this.SendToHandler(profile, rootPage, changedPages);
                            }
                        }
                    }
                }
            }
            if (num > 0)
                return num;
            return profile.SubscriptionInfo.SubscribedPages.Count != 0 ? 0 : -1;
        }

        /// <summary>
        /// Gets the page. Will be filtered on access rights, start publish and if the page is explicitly excluded.
        /// </summary>
        /// <param name="pageID">The ID of the page.</param>
        /// <param name="pageLanguages">The page languages. [Obsolete parameter]</param>
        /// <param name="language">The language you want the page in.</param>
        /// <param name="principal">The principal for the user. Used to check access rights on the page.</param>
        /// <returns>A PageData if one matching the criterias exists, otherwise null.</returns>
        [Obsolete("You must explicitly say if pages should be filtered or not to avoid bugs")]
        protected PageData GetPage(int pageID, IList<string> pageLanguages, string language, IPrincipal principal)
        {
            return this.GetPage(pageID, language, principal, true);
        }

        /// <summary>
        /// Gets the page. Will be filtered on access rights, start publish and if the page is explicitly excluded.
        /// </summary>
        /// <param name="pageID">The ID of the page.</param>
        /// <param name="language">The language you want the page in.</param>
        /// <param name="principal">The principal for the user. Used to check access rights on the page.</param>
        /// <param name="filterSubscribeExclude">Filter based on EPSUBSCRIBE-EXCLUDE</param>
        /// <returns>A PageData if one matching the criterias exists, otherwise null.</returns>
        protected PageData GetPage(int pageID, string language, IPrincipal principal, bool filterSubscribeExclude)
        {
            foreach (PageData page in new PageDataCollection(this._contentRepository.GetLanguageBranches<PageData>((ContentReference)new PageReference(pageID))))
            {
                DateTime? startPublish = page.StartPublish;
                DateTime now = DateTime.Now;
                if ((startPublish.HasValue ? (startPublish.GetValueOrDefault() > now ? 1 : 0) : 0) != 0)
                    return (PageData)null;
                if (((ILocalizable)page).Language.TwoLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    if ((page.GetSecurityDescriptor().GetAccessLevel(principal) & AccessLevel.Read) != AccessLevel.Read || filterSubscribeExclude && this.IsPageExcluded(page))
                        return (PageData)null;
                    return page;
                }
            }
            return (PageData)null;
        }

        /// <summary>
        /// Creates a dummy PageData object containing only a PageLanguageBranch property.
        /// </summary>
        private static PageData CreateLanguageBranchContainer(string language)
        {
            return new PageData((AccessControlList)null, new PropertyDataCollection()
            {
                {
                    "PageLanguageBranch",
                    (PropertyData) new PropertyString(language)
                }
            });
        }

        /// <summary>
        /// Gets the changed pages under one subscription root for a specific user.
        /// </summary>
        /// <param name="profile">The profile for a user.</param>
        /// <param name="language">The requested language.</param>
        /// <param name="subscribedPage">The subscribed subscription root.</param>
        /// <param name="principal">The principal for the user.</param>
        /// <returns>A collection of pages that have been changed and are children of the subscribedPage.</returns>
        protected PageDataCollection GetChangedPages(EPiServerProfile profile, string language, SubscriptionDescriptor subscribedPage, IPrincipal principal)
        {
            IList<PageLanguage> pageLanguageList = this._subscriptionDB.PagesChangedAfter(subscribedPage.PageID, profile.SubscriptionInfo.LastMessage, 100);
            int count = pageLanguageList.Count;
            int pageID = -1;
            PageDataCollection pageDataCollection = new PageDataCollection();
            IList<string> stringList = (IList<string>)new List<string>();
            foreach (PageLanguage pageLanguage in (IEnumerable<PageLanguage>)pageLanguageList)
            {
                if (string.Compare(pageLanguage.LanguageID, language, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (pageLanguage.PageID != pageID && pageID != -1)
                    {
                        PageData page = this.GetPage(pageID, pageLanguage.LanguageID, principal, true);
                        if (page != null)
                            pageDataCollection.Add(page);
                        stringList.Clear();
                    }
                    stringList.Add(pageLanguage.LanguageID);
                    pageID = pageLanguage.PageID;
                }
            }
            if (pageID != -1)
            {
                foreach (string language1 in (IEnumerable<string>)stringList)
                {
                    PageData page = this.GetPage(pageID, language1, principal, true);
                    if (page != null)
                        pageDataCollection.Add(page);
                }
            }
            return pageDataCollection;
        }

        /// <summary>
        /// Controls if a user can be notified about page changes. Called for each subscription being processed.
        /// </summary>
        /// <param name="profile">User's profile</param>
        /// <param name="rootPage">The root page for the suscription</param>
        /// <param name="changedPages">The pages that has changed</param>
        /// <returns>
        /// <b>True</b> if the subscription notification can be sent.
        ///     Returning <b>false</b> cancels the notification for this user.
        ///     </returns>
        protected virtual bool CanSendSubscription(EPiServerProfile profile, PageData rootPage, PageDataCollection changedPages)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified page should be excluded from the subscription.
        /// </summary>
        /// <param name="page">The page that may be excluded.</param>
        /// <returns>
        /// <c>true</c> if the page should be excluded; otherwise, <c>false</c>.
        ///     </returns>
        protected bool IsPageExcluded(PageData page)
        {
            if (page["EPSUBSCRIBE-EXCLUDE"] != null)
                return (bool)page["EPSUBSCRIBE-EXCLUDE"];
            return false;
        }

        /// <summary>Send subscription data to handler</summary>
        /// <param name="profile">The profile for a subscriber</param>
        /// <param name="rootPage">Root for subscription</param>
        /// <param name="changedPages">The pages that has changed</param>
        protected virtual void SendToHandler(EPiServerProfile profile, PageData rootPage, PageDataCollection changedPages)
        {
            IUpdateCurrentLanguage instance = ServiceLocator.Current.GetInstance<IUpdateCurrentLanguage>();
            if (this._languageBranchRepository.Load(profile.Language) != null)
                instance.UpdateLanguage(profile.Language);
            else
                instance.UpdateLanguage(ContentLanguage.Instance.FinalFallbackCulture.Name);
            string subscriptionHandler1 = SubscriptionJob.FindSubscriptionHandler(rootPage);
            SubscriptionJob.log.Debug((object)string.Format("Start processing subscription mail for {0}", (object)profile.DisplayName));
            SubscriptionJob.log.Debug((object)string.Format("Uses subscription handler: {0}", subscriptionHandler1.Length == 0 ? (object)"Default" : (object)subscriptionHandler1));
            ISubscriptionHandler subscriptionHandler2;
            if (string.IsNullOrEmpty(subscriptionHandler1))
            {
                subscriptionHandler2 = (ISubscriptionHandler)new Gosso.EpiserverLegacy.Personalization.Internal.SubscriptionMail(this._localizationService, this._siteDefinitionResolver);
            }
            else
            {
                subscriptionHandler2 = Activator.CreateInstance(Type.GetType(subscriptionHandler1, true, true)) as ISubscriptionHandler;
                if (subscriptionHandler2 == null)
                    throw new EPiServerException(string.Format("Failed to create a instance of \"{0}\", does it implement ISubscriptionHandler?", (object)Settings.Instance.SubscriptionHandler));
            }
            subscriptionHandler2.User = profile.UserName;
            subscriptionHandler2.UserData = profile;
            try
            {
                subscriptionHandler2.Send(rootPage, changedPages);
                SubscriptionJob.log.Debug((object)string.Format("Finished processing subscription mail for {0}", (object)profile.DisplayName));
            }
            catch (ConfigurationErrorsException ex)
            {
                SubscriptionJob.log.Error((object)string.Format("Failed to send subscription to {0} due to a configuration error", (object)profile.DisplayName), (Exception)ex);
                throw;
            }
            catch (SmtpException ex)
            {
                SubscriptionJob.log.Error((object)string.Format("Failed to send subscription to {0}", (object)profile.DisplayName), (Exception)ex);
                if (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                SubscriptionJob.log.Error((object)string.Format("Failed to send subscription to {0}", (object)profile.DisplayName), ex);
            }
        }

        /// <summary>
        /// Check if DateTime.Now is within the interval from the last e-mail sent
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="lastmessage">last message sent</param>
        /// <returns>
        /// <c>true</c> if DateTime.Now is within specified interval from the last e-mail sent; otherwise, <c>false</c>.
        ///     </returns>
        private static bool IsInInterval(int interval, DateTime lastmessage)
        {
            return lastmessage.AddDays((double)interval) >= DateTime.Now;
        }

        private static string FindSubscriptionHandler(PageData rootPage)
        {
            if ((ContentReference)rootPage.PageLink == (ContentReference)PageReference.EmptyReference)
                return string.Empty;

            //override with appsettings //future proof it
            var subscriptionHandler = ConfigurationManager.AppSettings["episerver:subscriptionHandler"] + "";
            if (!string.IsNullOrEmpty(subscriptionHandler))
                return subscriptionHandler;

            return Settings.Instance.SubscriptionHandler;
        }
    }
}
