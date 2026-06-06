using System;
using System.Collections.Generic;

namespace MunicipalTreasuryLedger
{
    public class AuditService
    {
        private readonly LedgerDatabase database;
        private readonly UserAccount currentUser;

        public AuditService(LedgerDatabase database, UserAccount currentUser)
        {
            this.database = database;
            this.currentUser = currentUser;
        }

        public AuditLogEntry Log(string action, string entityType, string entityId, string details)
        {
            return Log(action, entityType, entityId, details, null);
        }

        public AuditLogEntry Log(string action, string entityType, string entityId, string details, IEnumerable<AuditLogDetail> changeDetails)
        {
            if (database.AuditTrail == null)
            {
                database.AuditTrail = new List<AuditLogEntry>();
            }

            AuditLogEntry entry = new AuditLogEntry();
            entry.Username = currentUser == null ? "unknown" : currentUser.Username;
            entry.Role = currentUser == null ? "" : currentUser.Role;
            entry.Action = action;
            entry.EntityType = entityType;
            entry.EntityId = entityId;
            entry.Details = details;
            entry.ChangeDetails = new List<AuditLogDetail>();
            if (changeDetails != null)
            {
                foreach (AuditLogDetail detail in changeDetails)
                {
                    if (detail == null)
                    {
                        continue;
                    }

                    detail.AuditLogId = entry.Id;
                    if (String.IsNullOrEmpty(detail.Id))
                    {
                        detail.Id = Guid.NewGuid().ToString("N");
                    }

                    entry.ChangeDetails.Add(detail);
                }
            }

            database.AuditTrail.Add(entry);
            AuditHashService.SealNewEntry(database, entry);
            return entry;
        }

        public static AuditLogDetail Detail(string fieldName, string oldValue, string newValue)
        {
            return new AuditLogDetail
            {
                FieldName = fieldName ?? "",
                OldValue = oldValue ?? "",
                NewValue = newValue ?? ""
            };
        }

        public static string OwnerName(BusinessOwner owner)
        {
            if (owner == null)
            {
                return "";
            }

            string business = owner.BusinessName ?? "";
            string ownerName = owner.OwnerName ?? "";
            if (String.IsNullOrEmpty(business))
            {
                return ownerName;
            }

            if (String.IsNullOrEmpty(ownerName))
            {
                return business;
            }

            return business + " / " + ownerName;
        }
    }
}
