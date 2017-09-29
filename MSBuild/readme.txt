## Thanks for installing
## Remember the configuration

# Gosso.EpiserverLegacy.Personalization.Subscription
Project to replace legacy Episerver.Personalization.Subscription* objects

Just replace Episerver.Personalization.* with Gosso.EpiserverLegacy.Personalization.* where code obsolete or deprecated.

##Add your Subscription code with appsettings instead of episerver/applicationSettings
<appSettings>
  <add key="episerver:subscriptionHandler" value="Gosso.EpiserverLegacy.Personalization.Internal.SubscriptionMail,Gosso.EpiserverLegacy.Personalization.Subscription"/>
<appSettings>

##Deactivate default scheduled job
Under cms admin / config / plug-in manager / "episerver" / overview / Scheduler Job Administration / check out "Subscription"

Any bugs, you are welcome to contribute on github: or report it here:
https://github.com/LucGosso/Gosso.EpiserverLegacy.Personalization.Subscription/issues

Enjoy!  

More information https://github.com/LucGosso/Gosso.EpiserverLegacy.Personalization.Subscription