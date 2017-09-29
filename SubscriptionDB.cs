// Decompiled with JetBrains decompiler
// Type: EPiServer.DataAccess.Internal.SubscriptionDB
// Assembly: EPiServer, Version=10.9.1.0, Culture=neutral, PublicKeyToken=8fe83dea738b45b7
// MVID: 836E9D0C-CAF8-4929-A60A-D102446AC75C
// Assembly location: \packages\EPiServer.CMS.Core.10.9.1\lib\net45\EPiServer.dll

using EPiServer.Data;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using EPiServer.DataAccess;
using EPiServer.DataAccess.Internal;

namespace Gosso.EpiserverLegacy.DataAccess.Internal
{
    /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Database class for subscription data.
    /// </summary>
    /// <exclude />
    [ServiceConfiguration]
    //[Obsolete("The subscription feature was obsoleted in CMS 7.5 and is being phased out")]
    public class SubscriptionDB : DataAccessBase
    {
        private readonly DatabaseDateTimeHandler _dateTimeHandler;

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice.</summary>
        /// <internal-api />
        /// <exclude />
        public SubscriptionDB(EPiServer.Data.IDatabaseExecutor databaseHandler, DatabaseDateTimeHandler dateTimeHandler)
            : base(databaseHandler)
        {
            this._dateTimeHandler = dateTimeHandler;
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice.</summary>
        /// <internal-api />
        /// <exclude />
        public IList<int> ListSubscriptionRoots()
        {
            return this.Executor.Execute<IList<int>>((Func<IList<int>>)(() =>
            {
                List<int> intList = new List<int>();
                using (DbDataReader dbDataReader = this.CreateCommand("netSubscriptionListRoots").ExecuteReader())
                {
                    while (dbDataReader.Read())
                        intList.Add(dbDataReader.GetInt32(0));
                }
                return (IList<int>)intList;
            }));
        }

        //Missing netSubscriptionListRoots??
        //CREATE PROCEDURE [dbo].[netSubscriptionListRoots]
        //AS
        //BEGIN
        //	SELECT tblPage.pkID AS PageID
        //	FROM tblPage
        //	INNER JOIN tblProperty ON tblProperty.fkPageID		= tblPage.pkID
        //	INNER JOIN tblPageDefinition ON tblPageDefinition.pkID	= tblProperty.fkPageDefinitionID
        //	WHERE tblPageDefinition.Name='EPSUBSCRIBE-ROOT' AND NOT tblProperty.PageLink IS NULL AND tblPage.Deleted=0
        //END
        //GO

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. This member supports the EPiServer infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude />
        public IList<PageLanguage> PagesChangedAfter(int rootID, DateTime changedAfter, int maxHits)
        {
            return this.Executor.Execute<IList<PageLanguage>>((Func<IList<PageLanguage>>)(() =>
            {
                IList<PageLanguage> pageLanguageList = (IList<PageLanguage>)new List<PageLanguage>();
                DbCommand command = this.CreateCommand("netPagesChangedAfter");
                command.Parameters.Add((object)this.CreateParameter("RootID", (object)rootID));
                command.Parameters.Add((object)this.CreateParameter("ChangedAfter", (object)this._dateTimeHandler.ConvertToDatabase(changedAfter)));
                command.Parameters.Add((object)this.CreateParameter("MaxHits", (object)maxHits));
                command.Parameters.Add((object)this.CreateParameter("StopPublish", (object)this._dateTimeHandler.ConvertToDatabase(DateTime.Now)));
                using (DbDataReader dbDataReader = command.ExecuteReader())
                {
                    while (dbDataReader.Read())
                        pageLanguageList.Add(new PageLanguage(Convert.ToInt32(dbDataReader["PageID"]), dbDataReader["LanguageID"].ToString()));
                }
                return pageLanguageList;
            }));
        }

        //Missing netPagesChangedAfter??
        //SET ANSI_NULLS ON
        //GO
        //SET QUOTED_IDENTIFIER ON
        //GO
        //CREATE PROCEDURE [dbo].[netPagesChangedAfter333]
        //( 
        //	@RootID INT,
        //	@ChangedAfter DATETIME,
        //	@MaxHits INT,
        //	@StopPublish DATETIME
        //)
        //AS
        //BEGIN
        //	SET NOCOUNT ON
        //    SET @MaxHits = @MaxHits + 1 -- Return one more to determine if there are more pages to fetch (gets MaxHits + 1)
        //    SET ROWCOUNT @MaxHits

        //	SELECT 
        //	    tblPageLanguage.fkPageID AS PageID,
        //		RTRIM(tblLanguageBranch.LanguageID) AS LanguageID
        //	FROM
        //		tblPageLanguage
        //	INNER JOIN
        //		tblTree
        //	ON
        //		tblPageLanguage.fkPageID = tblTree.fkChildID AND (tblTree.fkParentID = @RootID OR (tblTree.fkChildID = @RootID AND tblTree.NestingLevel = 1))
        //	INNER JOIN
        //		tblLanguageBranch
        //	ON
        //		tblPageLanguage.fkLanguageBranchID = tblLanguageBranch.pkID
        //	WHERE
        //		(tblPageLanguage.Changed > @ChangedAfter OR tblPageLanguage.StartPublish > @ChangedAfter) AND
        //		(tblPageLanguage.StopPublish is NULL OR tblPageLanguage.StopPublish > @StopPublish) AND
        //		tblPageLanguage.PendingPublish=0
        //	ORDER BY
        //		tblTree.NestingLevel,
        //		tblPageLanguage.fkPageID,
        //		tblPageLanguage.Changed DESC

        //	SET ROWCOUNT 0
        //END



    }
}
