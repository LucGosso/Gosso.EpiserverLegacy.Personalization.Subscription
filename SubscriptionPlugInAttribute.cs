// Decompiled with JetBrains decompiler
// Type: EPiServer.Personalization.SubscriptionPlugInAttribute
// Assembly: EPiServer, Version=10.10.1.0, Culture=neutral, PublicKeyToken=8fe83dea738b45b7
// MVID: 1E5E6E8B-1F89-4CA6-B156-113C5229AAD8
// Assembly location: \packages\EPiServer.CMS.Core.10.10.1\lib\net45\EPiServer.dll

using EPiServer.PlugIn;
using System;

namespace Gosso.EpiserverLegacy.Personalization
{
    /// <summary>
    /// A plug-in attribute to have a custom subscription sender class available under system settings.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///       A subscription plug-in must implement the <see cref="T:EPiServer.Personalization.ISubscriptionHandler" /> interface.
    ///       </para>
    ///   <para>
    ///     <note>
    ///       This plug-in will not activate this handler, it will only show up as a new
    ///       alternative that have to be manually selected under System settings in administration mode.
    ///       </note>
    ///   </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    //[Obsolete("The subscription feature was obsoleted in CMS 7.5 and is being phased out")]
    public class SubscriptionPlugInAttribute : PlugInAttribute
    {
        /// <summary>
        /// Checks if the specified object is of this typs (SubscriptionPlugInAttribute).
        /// </summary>
        /// <param name="o">The object to match.</param>
        /// <returns>True if the specified object is of the type SubscriptionPlugInAttribute, otherwis false.</returns>
        public override bool Match(object o)
        {
            return o is SubscriptionPlugInAttribute;
        }
    }
}
