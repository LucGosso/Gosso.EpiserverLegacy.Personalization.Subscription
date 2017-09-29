using EPiServer.Core;
using EPiServer.Personalization;
using System;

namespace Gosso.EpiserverLegacy.Personalization
{
    /// <summary>
    /// Interface to support sending of customized subscriptions
    /// </summary>
    //[Obsolete("The subscription feature was obsoleted in CMS 7.5 and is being phased out")]
    public interface ISubscriptionHandler
    {
        /// <summary>Gets or sets the user name.</summary>
        /// <value>The user name.</value>
        string User { get; set; }

        /// <summary>Gets or sets the personilized data of the recipient.</summary>
        /// <value>The personilized data.</value>
        EPiServerProfile UserData { get; set; }

        /// <summary>
        /// Sends the subscriptions for the specified subscription page.
        /// </summary>
        /// <param name="subscriptionPage">The root page for the subscription that this mail should be based on.</param>
        /// <param name="updatedPages">The changed pages to notify the user about.</param>
        void Send(PageData subscriptionPage, PageDataCollection updatedPages);
    }
}
