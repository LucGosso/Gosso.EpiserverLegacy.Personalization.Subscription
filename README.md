# Gosso.EpiserverLegacy.Personalization.Subscription
Project to replace legacy Episerver.Personalization.Subscription* objects

[![Platform](https://img.shields.io/badge/Episerver-%2010.9+-green.svg?style=flat)](https://world.episerver.com/cms/)

Just replace Episerver.Personalization.* with Gosso.EpiserverLegacy.Personalization.* where code obsolete or deprecated.

## Add your Subscription code with appsettings instead of episerver/applicationSettings
<appSettings>
  <add key="episerver:subscriptionHandler" value="Gosso.EpiserverLegacy.Personalization.Internal.SubscriptionMail,Gosso.EpiserverLegacy.Personalization.Subscription"/>
<appSettings>

## Deactivate default scheduled job
Under cms admin / config / plug-in manager / "episerver" / overview / Scheduler Job Administration / check out "Subscription"

# Installation

To be written

## Any bugs, you are welcome to contribute

# What is the subscription system?

Subscription is a legacy system in Episerver from version 4 (back in 2004). This is since version 7.5 obsolete, and removed? in version 11. Why you think? it is undocumented, does not scale well, does not support SSO and does not support a typed model.

Subscription allows website visitors to subscribe by e-mail to website content of their choice. Selected content pages, for instance news or event pages, can be made available to be included in the subscription scheduled job that is set up. This document describes the subscription feature in in EPiServer CMS, and how to configure the look and content of subscription e-mails sent out.

Resources:
https://world.episerver.com/documentation/Items/Developers-Guide/Episerver-CMS/7/Subscription/Subscription/
https://world.episerver.com/globalassets/documents/the_book/the_total_book_developing-solutions-with-episerver-4.pdf