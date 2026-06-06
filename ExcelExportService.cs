using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public static class ExcelExportService
    {
        public static void ExportLedger(LedgerDatabase database, string filePath)
        {
            ExportRows(BuildLedgerRows(database, null), filePath, "Ledger");
        }

        public static void ExportArchive(LedgerDatabase database, string filePath, int throughYear)
        {
            ExportRows(BuildLedgerRows(database, throughYear), filePath, "Archive");
        }

        private static List<List<string>> BuildLedgerRows(LedgerDatabase database, int? throughYear)
        {
            List<List<string>> rows = new List<List<string>>();
            rows.Add(new List<string>
            {
                "Owner Name",
                "Business Name",
                "Line of Business",
                "Status",
                "Registration Type",
                "Privacy Consent",
                "Consent Date",
                "Consent Method",
                "Privacy Notice Version",
                "Year",
                "Capital",
                "Gross Sales",
                "Business Tax",
                "Mayor's Permit",
                "Fees",
                "Surcharge",
                "Penalty",
                "Total Assessment",
                "Amount Paid",
                "Balance",
                "Payment Date",
                "OR Number",
                "Payment Schedule",
                "Payment Amount",
                "Payment Remarks"
            });

            if (database == null || database.Owners == null)
            {
                return rows;
            }

            foreach (BusinessOwner owner in database.Owners.OrderBy(owner => Safe(owner.BusinessName)).ThenBy(owner => Safe(owner.OwnerName)))
            {
                List<YearlyAssessment> assessments = owner.Assessments == null
                    ? new List<YearlyAssessment>()
                    : owner.Assessments
                        .Where(assessment => !throughYear.HasValue || assessment.Year <= throughYear.Value)
                        .OrderByDescending(assessment => assessment.Year)
                        .ToList();

                if (assessments.Count == 0)
                {
                    if (!throughYear.HasValue)
                    {
                        rows.Add(BuildRow(owner, null, null));
                    }

                    continue;
                }

                foreach (YearlyAssessment assessment in assessments)
                {
                    if (assessment.Payments == null || assessment.Payments.Count == 0)
                    {
                        rows.Add(BuildRow(owner, assessment, null));
                        continue;
                    }

                    foreach (PaymentRecord payment in assessment.Payments.OrderBy(payment => payment.DatePaid).ThenBy(payment => Safe(payment.OrNumber)))
                    {
                        rows.Add(BuildRow(owner, assessment, payment));
                    }
                }
            }

            return rows;
        }

        private static List<string> BuildRow(BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment)
        {
            return new List<string>
            {
                Safe(owner == null ? "" : owner.OwnerName),
                Safe(owner == null ? "" : owner.BusinessName),
                Safe(owner == null ? "" : owner.LineOfBusiness),
                Safe(owner == null ? "" : owner.Status),
                Safe(owner == null ? "" : owner.RegistrationType),
                owner != null && owner.PrivacyConsentGiven ? "Yes" : "No",
                owner == null || owner.PrivacyConsentDate == DateTime.MinValue ? "" : owner.PrivacyConsentDate.ToString("yyyy-MM-dd"),
                Safe(owner == null ? "" : owner.PrivacyConsentMethod),
                Safe(owner == null ? "" : owner.PrivacyNoticeVersion),
                assessment == null ? "" : assessment.Year.ToString(CultureInfo.InvariantCulture),
                Money(assessment == null ? 0m : assessment.Capital),
                Money(assessment == null ? 0m : assessment.GrossSales),
                Money(assessment == null ? 0m : assessment.BusinessTax),
                Money(assessment == null ? 0m : assessment.MayorsPermit),
                Money(assessment == null ? 0m : assessment.Fees),
                Money(assessment == null ? 0m : assessment.Surcharge),
                Money(assessment == null ? 0m : assessment.Penalty),
                Money(assessment == null ? 0m : assessment.TotalAssessment),
                Money(assessment == null ? 0m : assessment.TotalPaid),
                Money(assessment == null ? 0m : assessment.Balance),
                payment == null ? "" : payment.DatePaid.ToString("yyyy-MM-dd"),
                Safe(payment == null ? "" : payment.OrNumber),
                Safe(payment == null ? "" : payment.Schedule),
                payment == null ? "" : Money(payment.Amount),
                Safe(payment == null ? "" : payment.Remarks)
            };
        }

        private static void ExportRows(List<List<string>> rows, string filePath, string sheetName)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (FileStream stream = File.Create(filePath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                AddText(archive, "[Content_Types].xml", ContentTypesXml());
                AddText(archive, "_rels/.rels", RootRelationshipsXml());
                AddText(archive, "xl/workbook.xml", WorkbookXml(sheetName));
                AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
                AddText(archive, "xl/styles.xml", StylesXml());
                AddText(archive, "xl/worksheets/sheet1.xml", WorksheetXml(rows));
            }
        }

        private static string WorksheetXml(List<List<string>> rows)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            builder.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            builder.Append("<sheetData>");

            for (int r = 0; r < rows.Count; r++)
            {
                builder.Append("<row r=\"").Append(r + 1).Append("\">");
                List<string> cells = rows[r];
                for (int c = 0; c < cells.Count; c++)
                {
                    builder.Append("<c r=\"").Append(CellReference(r + 1, c + 1)).Append("\" t=\"inlineStr\"");
                    if (r == 0)
                    {
                        builder.Append(" s=\"1\"");
                    }

                    builder.Append("><is><t>");
                    builder.Append(Xml(cells[c]));
                    builder.Append("</t></is></c>");
                }

                builder.Append("</row>");
            }

            builder.Append("</sheetData>");
            if (rows.Count > 0 && rows[0].Count > 0)
            {
                builder.Append("<autoFilter ref=\"A1:").Append(CellReference(Math.Max(1, rows.Count), rows[0].Count)).Append("\"/>");
            }

            builder.Append("</worksheet>");
            return builder.ToString();
        }

        private static string ContentTypesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>";
        }

        private static string RootRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private static string WorkbookXml(string sheetName)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"" + XmlAttribute(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>";
        }

        private static string WorkbookRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>";
        }

        private static string StylesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
                "</styleSheet>";
        }

        private static void AddText(ZipArchive archive, string path, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (Stream entryStream = entry.Open())
            using (StreamWriter writer = new StreamWriter(entryStream, Encoding.UTF8))
            {
                writer.Write(text);
            }
        }

        private static string CellReference(int row, int column)
        {
            string letters = "";
            while (column > 0)
            {
                int remainder = (column - 1) % 26;
                letters = (char)('A' + remainder) + letters;
                column = (column - 1) / 26;
            }

            return letters + row.ToString(CultureInfo.InvariantCulture);
        }

        private static string Xml(string value)
        {
            return (value ?? "")
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static string XmlAttribute(string value)
        {
            return Xml(value).Replace("\"", "&quot;");
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Safe(string value)
        {
            return value ?? "";
        }
    }
}
