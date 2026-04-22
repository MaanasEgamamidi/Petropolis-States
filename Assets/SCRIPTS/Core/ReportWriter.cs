using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Generates numbered HTML session report files in a <c>Session Reports/</c> folder at the project root.
/// Uses Chart.js (CDN) with per-segment line coloring: green = gain, red = loss, gray = flat.
/// X-axis uses cumulative hours with 3-hour aligned ticks and day boundary labels; pet stats share one multi-line chart with a legend.
/// Includes distance traveled with the pet (player planar movement while the pet is active), an NPC chat
/// transcript (<c>You:</c> / <c>NPC:</c> lines) from <see cref="SessionChatLog"/>.
/// </summary>
public static class ReportWriter
{
    const string ReportFilePrefix = "SessionReport";

    public static void Generate(
        string playerName,
        string petName,
        float sessionRealSeconds,
        float totalGameHours,
        float distanceWithPetMeters,
        float startingMoney,
        float endingMoney,
        float highestMoney,
        float lowestMoney,
        List<SessionTracker.MoneySnapshot> snapshots,
        IReadOnlyList<SessionChatLog.Entry> playerChatLog,
        IReadOnlyDictionary<BadgeId, int> earnedBadges = null,
        IReadOnlyList<SessionTracker.PurchaseEntry> purchases = null,
        IReadOnlyList<SessionTracker.FeedEntry> feedings = null)
    {
        string reportsDir = Path.Combine(Application.dataPath, "..", "Session Reports");
        Directory.CreateDirectory(reportsDir);

        int reportNumber = NextReportNumber(reportsDir);
        string filePath = Path.Combine(reportsDir, $"{ReportFilePrefix}{reportNumber}.html");

        string sessionLength = FormatSessionLength(totalGameHours);
        float netChange = endingMoney - startingMoney;

        float startHour = snapshots.Count > 0 ? snapshots[0].GameHour : 0f;
        string pointsJs = BuildPointsArray(snapshots);
        string healthJs = BuildStatPointsArray(snapshots, s => s.PetHealth);
        string happinessJs = BuildStatPointsArray(snapshots, s => s.PetHappiness);
        string energyJs = BuildStatPointsArray(snapshots, s => s.PetEnergy);
        string hungerJs = BuildStatPointsArray(snapshots, s => s.PetHunger);

        string reportTimestamp = FormatWashingtonLocalTimestamp(DateTime.UtcNow);
        string chatLogHtml = BuildPlayerChatLogHtml(playerChatLog);
        string distanceStr = FormatDistanceMeters(distanceWithPetMeters);
        string badgesHtml = BuildBadgesHtml(earnedBadges);
        string purchasesHtml = BuildPurchasesHtml(purchases, startHour);
        string feedingsHtml = BuildFeedingsHtml(feedings, startHour);
        string html = BuildHtml(
            reportTimestamp, playerName, petName, sessionLength, distanceStr,
            startingMoney, endingMoney, netChange, highestMoney, lowestMoney,
            pointsJs, healthJs, happinessJs, energyJs, hungerJs, startHour, totalGameHours,
            chatLogHtml, badgesHtml, purchasesHtml, feedingsHtml);

        File.WriteAllText(filePath, html, Encoding.UTF8);
        Debug.Log($"[SessionReport] Saved report to {filePath}");
    }

    static int NextReportNumber(string dir)
    {
        int max = 0;
        if (Directory.Exists(dir))
        {
            foreach (string file in Directory.GetFiles(dir, $"{ReportFilePrefix}*.html"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith(ReportFilePrefix) &&
                    int.TryParse(name.AsSpan(ReportFilePrefix.Length), out int n))
                {
                    if (n > max) max = n;
                }
            }
        }
        return max + 1;
    }

    /// <summary>US Pacific (America/Los_Angeles) — same civil time as Washington state.</summary>
    static TimeZoneInfo WashingtonTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        }
    }

    static string FormatWashingtonLocalTimestamp(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
            utcNow = utcNow.ToUniversalTime();

        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, WashingtonTimeZone());
        return local.ToString("MMMM d, yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    static string FormatDistanceMeters(float meters)
    {
        if (float.IsNaN(meters) || float.IsInfinity(meters) || meters < 0f)
            meters = 0f;
        if (meters >= 1000f)
            return $"{meters / 1000f:0.##} km";
        return $"{meters:0.#} m";
    }

    static string FormatSessionLength(float gameHours)
    {
        if (gameHours >= 24f)
        {
            float days = gameHours / 24f;
            return $"{days:0.#} in-game day{(days >= 2f ? "s" : "")}";
        }
        return $"{gameHours:0.#} in-game hour{(gameHours >= 2f ? "s" : "")}";
    }

    static string BuildPointsArray(List<SessionTracker.MoneySnapshot> snapshots)
    {
        var sb = new StringBuilder("[");
        float cumHours = 0f;
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (i > 0)
            {
                float diff = snapshots[i].GameHour - snapshots[i - 1].GameHour;
                if (diff < 0f) diff += 24f;
                cumHours += diff;
                sb.Append(',');
            }
            sb.Append($"{{x:{cumHours:0.##},y:{snapshots[i].Money:0.00}}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string BuildStatPointsArray(List<SessionTracker.MoneySnapshot> snapshots, Func<SessionTracker.MoneySnapshot, float> getY)
    {
        var sb = new StringBuilder("[");
        float cumHours = 0f;
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (i > 0)
            {
                float diff = snapshots[i].GameHour - snapshots[i - 1].GameHour;
                if (diff < 0f) diff += 24f;
                cumHours += diff;
                sb.Append(',');
            }
            sb.Append($"{{x:{cumHours:0.##},y:{getY(snapshots[i]):0.##}}}");
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string BuildPlayerChatLogHtml(IReadOnlyList<SessionChatLog.Entry> entries)
    {
        if (entries == null || entries.Count == 0)
            return "<p class=\"chat-transcript-empty\">No NPC chat this session.</p>";

        var sorted = new List<SessionChatLog.Entry>(entries);
        sorted.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

        var sb = new StringBuilder();
        sb.Append("<div class=\"chat-transcript\">\n");

        string prevPartner = null;
        foreach (SessionChatLog.Entry e in sorted)
        {
            string partner = string.IsNullOrEmpty(e.NpcName) ? "NPC" : e.NpcName;
            if (prevPartner != partner)
            {
                if (prevPartner != null)
                    sb.Append("  </div>\n");
                sb.Append("  <div class=\"chat-npc-block\">\n");
                sb.Append($"    <div class=\"chat-partner\"><strong>{Escape(partner)}</strong></div>\n");
                prevPartner = partner;
            }

            if (e.FromPlayer)
            {
                sb.Append($"    <div class=\"chat-line chat-you\"><strong>You</strong>: {Escape(e.Message)}</div>\n");
            }
            else if (e.Purchases != null && e.Purchases.Count > 0)
            {
                sb.Append("    <div class=\"chat-line chat-npc chat-transaction\">")
                  .Append("<span class=\"chat-tx-badge\">Transaction</span>")
                  .Append($"<strong>{Escape(partner)}</strong>: {Escape(e.Message)}")
                  .Append(BuildTransactionTooltip(e.Purchases))
                  .Append("</div>\n");
            }
            else
            {
                sb.Append($"    <div class=\"chat-line chat-npc\"><strong>{Escape(partner)}</strong>: {Escape(e.Message)}</div>\n");
            }
        }

        if (prevPartner != null)
            sb.Append("  </div>\n");
        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>
    /// Builds the hover tooltip listing every item that changed hands inside a single NPC reply,
    /// with quantity, unit price, and line total — rendered next to the "Transaction" badge.
    /// </summary>
    static string BuildTransactionTooltip(IReadOnlyList<SessionChatLog.Purchase> purchases)
    {
        var sb = new StringBuilder();
        sb.Append("      <div class=\"chat-tx-tooltip\">\n");
        sb.Append("        <div class=\"tt-title\">Transaction</div>\n");
        sb.Append("        <table class=\"tt-table\"><tr><th>Item</th><th>Qty</th><th>Unit</th><th>Total</th></tr>\n");

        float grandTotal = 0f;
        for (int i = 0; i < purchases.Count; i++)
        {
            SessionChatLog.Purchase p = purchases[i];
            float lineTotal = p.PricePerUnit * p.Quantity;
            grandTotal += lineTotal;

            sb.Append("          <tr>")
              .Append("<td>").Append(Escape(p.ItemName)).Append("</td>")
              .Append("<td>×").Append(p.Quantity.ToString(CultureInfo.InvariantCulture)).Append("</td>")
              .Append("<td>$").Append(p.PricePerUnit.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>")
              .Append("<td>$").Append(lineTotal.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>")
              .Append("</tr>\n");
        }

        sb.Append("        </table>\n");
        sb.Append("        <div class=\"tt-total\">Total paid: $")
          .Append(grandTotal.ToString("0.00", CultureInfo.InvariantCulture))
          .Append("</div>\n");
        sb.Append("      </div>\n");
        return sb.ToString();
    }

    /// <summary>Same clock math as the chart's JS <c>fmtHour</c> / <c>dayNum</c>. Produces e.g. <c>Day 1, 7:32 AM</c>.</summary>
    static string FormatInGameClock(float gameHour, float startHour)
    {
        float cumHours = gameHour - startHour;
        if (cumHours < 0f) cumHours += 24f;

        float hod = (startHour + cumHours) % 24f;
        if (hod < 0f) hod += 24f;

        int dayNum = (int)Math.Floor((startHour + cumHours) / 24f) + 1;

        int wholeHour = (int)Math.Floor(hod);
        int minutes = (int)Math.Round((hod - wholeHour) * 60f);
        if (minutes >= 60) { minutes = 0; wholeHour = (wholeHour + 1) % 24; }

        int h12 = wholeHour % 12;
        if (h12 == 0) h12 = 12;
        string suffix = wholeHour < 12 ? "AM" : "PM";
        return $"Day {dayNum}, {h12}:{minutes:00} {suffix}";
    }

    static string FormatElapsedSeconds(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) seconds = 0f;
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1d)
            return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        return $"{t.Minutes:00}:{t.Seconds:00}";
    }

    static string BuildPurchasesHtml(IReadOnlyList<SessionTracker.PurchaseEntry> purchases, float startHour)
    {
        if (purchases == null || purchases.Count == 0)
            return "<p class=\"report-empty\">No purchases this session.</p>";

        var sb = new StringBuilder();
        sb.Append("<table class=\"report-table\">\n");
        sb.Append("  <tr><th>Item</th><th>Qty</th><th>Unit Price</th><th>Total</th><th>When (in-game)</th><th>Elapsed (real)</th></tr>\n");

        int totalQty = 0;
        float totalSpent = 0f;
        for (int i = 0; i < purchases.Count; i++)
        {
            SessionTracker.PurchaseEntry p = purchases[i];
            float lineTotal = p.PricePerUnit * p.Quantity;
            totalQty += p.Quantity;
            totalSpent += lineTotal;

            sb.Append("  <tr>");
            sb.Append("<td>").Append(Escape(p.ItemName)).Append("</td>");
            sb.Append("<td>").Append(p.Quantity.ToString(CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td>$").Append(p.PricePerUnit.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td>$").Append(lineTotal.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td>").Append(Escape(FormatInGameClock(p.GameHour, startHour))).Append("</td>");
            sb.Append("<td>").Append(Escape(FormatElapsedSeconds(p.RealSecondsSinceStart))).Append("</td>");
            sb.Append("</tr>\n");
        }

        sb.Append("  <tr><th>Totals</th>");
        sb.Append("<td>").Append(totalQty.ToString(CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td></td>");
        sb.Append("<td>$").Append(totalSpent.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td>");
        sb.Append("<td></td><td></td></tr>\n");
        sb.Append("</table>");
        return sb.ToString();
    }

    static string BuildFeedingsHtml(IReadOnlyList<SessionTracker.FeedEntry> feedings, float startHour)
    {
        if (feedings == null || feedings.Count == 0)
            return "<p class=\"report-empty\">No feedings this session.</p>";

        var sb = new StringBuilder();
        sb.Append("<table class=\"report-table\">\n");
        sb.Append("  <tr><th>#</th><th>Food</th><th>When (in-game)</th><th>Elapsed (real)</th></tr>\n");

        for (int i = 0; i < feedings.Count; i++)
        {
            SessionTracker.FeedEntry f = feedings[i];
            sb.Append("  <tr>");
            sb.Append("<td>").Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td>").Append(Escape(f.FoodName)).Append("</td>");
            sb.Append("<td>").Append(Escape(FormatInGameClock(f.GameHour, startHour))).Append("</td>");
            sb.Append("<td>").Append(Escape(FormatElapsedSeconds(f.RealSecondsSinceStart))).Append("</td>");
            sb.Append("</tr>\n");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    static string BuildBadgesHtml(IReadOnlyDictionary<BadgeId, int> earnedBadges)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"badges-grid\">\n");

        foreach (var kvp in BadgeDefinitions.All)
        {
            BadgeDefinition def = kvp.Value;
            int earnedTier = BadgeTier.None;
            if (earnedBadges != null && earnedBadges.TryGetValue(def.Id, out int t))
                earnedTier = t;

            // Pick which image to show: highest earned tier; otherwise the lowest-tier image (tinted dark).
            int imageIndex;
            if (def.IsTiered)
            {
                imageIndex = earnedTier > 0 ? Mathf.Clamp(earnedTier - 1, 0, def.TierImagePaths.Length - 1) : 0;
            }
            else
            {
                imageIndex = 0;
            }

            string imageRel = "../" + def.TierImagePaths[imageIndex].Replace('\\', '/');
            bool earned = earnedTier > 0 && !def.NotImplemented;

            string statusLabel = def.IsTiered
                ? BadgeDefinitions.TierName(earnedTier) + " Tier"
                : "Earned!";

            string tileClass = earned ? "badge-tile earned" : (def.NotImplemented ? "badge-tile locked soon" : "badge-tile locked");

            sb.Append("  <div class=\"").Append(tileClass).Append("\">\n");

            if (earned)
            {
                sb.Append("    <img class=\"badge-img\" src=\"").Append(Escape(imageRel)).Append("\" alt=\"").Append(Escape(def.DisplayName)).Append("\" />\n");
            }
            else
            {
                string placeholderText = def.NotImplemented ? "Coming soon" : "Not earned yet";
                sb.Append("    <div class=\"badge-placeholder\"><span>").Append(Escape(placeholderText)).Append("</span></div>\n");
            }

            sb.Append("    <div class=\"badge-name\">").Append(Escape(def.DisplayName)).Append("</div>\n");
            if (earned)
                sb.Append("    <div class=\"badge-status earned\">").Append(Escape(statusLabel)).Append("</div>\n");

            sb.Append("    <div class=\"badge-tooltip\">\n");
            sb.Append("      <div class=\"tt-title\">").Append(Escape(def.DisplayName)).Append("</div>\n");
            sb.Append("      <div class=\"tt-sub\">").Append(Escape(def.Category)).Append("</div>\n");
            sb.Append("      <ul class=\"tt-list\">\n");
            for (int i = 0; i < def.TierCriteria.Length; i++)
                sb.Append("        <li>").Append(Escape(def.TierCriteria[i])).Append("</li>\n");
            sb.Append("      </ul>\n");
            if (def.NotImplemented)
                sb.Append("      <div class=\"tt-note\">Tracking not implemented yet — always shown as not earned.</div>\n");
            sb.Append("    </div>\n");

            sb.Append("  </div>\n");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    static string BuildHtml(
        string reportTimestamp,
        string playerName, string petName, string sessionLength,
        string distanceWithPetDisplay,
        float startingMoney, float endingMoney, float netChange,
        float highestMoney, float lowestMoney,
        string pointsJs,
        string healthJs, string happinessJs, string energyJs, string hungerJs,
        float startHour, float totalGameHours,
        string playerChatLogHtml, string badgesHtml,
        string purchasesHtml, string feedingsHtml)
    {
        string netSign = netChange >= 0 ? "+" : "";
        string netColor = netChange > 0 ? "#2a7d2a" : netChange < 0 ? "#a33" : "#444";
        string showDays = totalGameHours >= 20f ? "true" : "false";
        string startHourStr = startHour.ToString("0.##");

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Session Report — {Escape(reportTimestamp)}</title>
<style>
  * {{ box-sizing: border-box; }}
  body {{
    font-family: 'Segoe UI', Arial, sans-serif;
    background: #f4f4f4;
    color: #333;
    max-width: 960px;
    margin: 36px auto;
    padding: 0 24px;
    line-height: 1.5;
  }}
  h1 {{
    font-size: 1.35em;
    font-weight: 600;
    border-bottom: 2px solid #d0d0d0;
    padding-bottom: 8px;
    margin-bottom: 4px;
  }}
  .report-ts {{
    margin: 0 0 22px 0;
    color: #666;
    font-size: 0.95em;
  }}
  h2 {{
    font-size: 1.05em;
    font-weight: 600;
    margin-top: 26px;
    margin-bottom: 8px;
  }}
  table {{
    border-collapse: collapse;
    width: 100%;
    margin-bottom: 18px;
    background: #fff;
    border-radius: 4px;
    overflow: hidden;
    border: 1px solid #e0e0e0;
  }}
  th, td {{
    text-align: left;
    padding: 8px 14px;
    border-bottom: 1px solid #eee;
  }}
  tr:last-child th, tr:last-child td {{ border-bottom: none; }}
  th {{
    font-weight: 600;
    width: 42%;
    color: #555;
  }}
  .net {{
    color: {netColor};
    font-weight: 600;
  }}
  .chart-wrap {{
    background: #fff;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 16px 18px 10px;
    margin-top: 10px;
  }}
   .chat-section {{
    background: #f9f9f9;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 14px 18px 12px;
    margin-top: 10px;
    font-size: 0.95em;
  }}
  .chat-transcript {{
    white-space: pre-wrap;
    word-break: break-word;
  }}
  .chat-npc-block {{
    background: #fff;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 12px 14px 10px;
    margin-top: 12px;
  }}
  .chat-npc-block:first-child {{ margin-top: 0; }}
  .chat-partner {{
    font-size: 1em;
    color: #444;
    margin: 0 0 0.08em 0;
    line-height: 1.35;
  }}
  .chat-line {{
    margin: 0 0 0.15em 0;
    padding: 0;
    line-height: 1.35;
  }}
  .chat-npc-block .chat-line:last-child {{ margin-bottom: 0; }}
  .chat-you {{ color: #333; }}
  .chat-npc {{ color: #333; }}
  .chat-line strong {{ font-weight: 700; }}
  .chat-transaction {{
    position: relative;
    background: #eaf7ea;
    border-left: 3px solid #3a9d4a;
    padding: 5px 8px 5px 10px;
    margin: 4px 0;
    border-radius: 3px;
    cursor: help;
    white-space: normal;
  }}
  .chat-tx-badge {{
    display: inline-block;
    background: #3a9d4a;
    color: #fff;
    font-size: 0.72em;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    padding: 2px 6px;
    border-radius: 3px;
    line-height: 1.2;
    margin-right: 6px;
    vertical-align: middle;
  }}
  .chat-tx-tooltip {{
    position: absolute;
    bottom: calc(100% + 8px);
    left: 12px;
    min-width: 260px;
    max-width: 360px;
    background: #2b2b2b;
    color: #fff;
    padding: 10px 12px 10px;
    border-radius: 6px;
    box-shadow: 0 6px 18px rgba(0,0,0,0.22);
    font-size: 0.88em;
    line-height: 1.4;
    text-align: left;
    opacity: 0;
    pointer-events: none;
    transform: translateY(4px);
    transition: opacity 0.12s ease, transform 0.12s ease;
    z-index: 20;
  }}
  .chat-tx-tooltip::after {{
    content: '';
    position: absolute;
    top: 100%;
    left: 24px;
    border: 6px solid transparent;
    border-top-color: #2b2b2b;
  }}
  .chat-transaction:hover .chat-tx-tooltip {{
    opacity: 1;
    transform: translateY(0);
  }}
  .chat-tx-tooltip .tt-title {{
    font-weight: 700;
    color: #7fd88a;
    font-size: 0.9em;
    letter-spacing: 0.03em;
    text-transform: uppercase;
    margin-bottom: 6px;
  }}
  .chat-tx-tooltip .tt-table {{
    width: 100%;
    border-collapse: collapse;
    background: transparent;
    border: none;
    margin: 0;
  }}
  .chat-tx-tooltip .tt-table th,
  .chat-tx-tooltip .tt-table td {{
    padding: 3px 8px 3px 0;
    border: none;
    border-bottom: 1px solid rgba(255,255,255,0.08);
    color: #fff;
    background: transparent;
    font-size: 0.95em;
    text-align: left;
    width: auto;
  }}
  .chat-tx-tooltip .tt-table tr:last-child td {{ border-bottom: none; }}
  .chat-tx-tooltip .tt-table th {{
    color: #aaa;
    font-weight: 500;
    font-size: 0.8em;
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }}
  .chat-tx-tooltip .tt-total {{
    margin-top: 6px;
    padding-top: 6px;
    border-top: 1px solid rgba(255,255,255,0.15);
    font-weight: 700;
    color: #7fd88a;
  }}
  .chat-transcript-empty {{
    color: #666;
    font-size: 0.95em;
    margin: 0;
  }}
  .report-empty {{
    color: #666;
    font-size: 0.95em;
    background: #fff;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 12px 14px;
    margin: 6px 0 18px 0;
  }}
  table.report-table th {{ width: auto; font-weight: 600; color: #333; background: #f5f5f5; }}
  table.report-table td {{ color: #333; }}
  table.report-table td:nth-child(n+2), table.report-table th:nth-child(n+2) {{ white-space: nowrap; }}
  .badges-grid {{
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
    background: #fff;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    padding: 18px;
    margin-top: 10px;
    overflow: visible;
  }}
  @media (max-width: 720px) {{
    .badges-grid {{ grid-template-columns: repeat(2, 1fr); }}
  }}
  .badge-tile {{
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    padding: 10px 8px 12px;
    border-radius: 6px;
    background: #fff;
    border: 1px solid #ececec;
    cursor: help;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }}
  .badge-tile:hover {{
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  }}
  .badge-img {{
    width: 100%;
    max-width: 140px;
    height: auto;
    aspect-ratio: 1 / 1;
    object-fit: contain;
    margin-bottom: 8px;
  }}
  .badge-placeholder {{
    width: 100%;
    max-width: 140px;
    aspect-ratio: 1 / 1;
    margin-bottom: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #eef0f2;
    border: 1px dashed #c9ced3;
    border-radius: 8px;
    color: #7a828b;
    font-size: 1.05em;
    font-weight: 600;
    padding: 8px;
  }}
  .badge-placeholder span {{
    display: block;
    line-height: 1.25;
  }}
  .badge-tile.soon .badge-placeholder {{
    background: #fdf4e3;
    border-color: #e7d2a3;
    color: #b88a2a;
  }}
  .badge-name {{
    font-size: 0.95em;
    font-weight: 600;
    color: #333;
    margin-bottom: 2px;
  }}
  .badge-tile.locked .badge-name {{ color: #555; }}
  .badge-status {{
    font-size: 0.82em;
    font-weight: 500;
  }}
  .badge-status.earned {{ color: #2a7d2a; }}
  .badge-status.locked {{ color: #999; }}
  .badge-status.soon {{ color: #b88a2a; }}

  .badge-tooltip {{
    position: absolute;
    bottom: calc(100% + 10px);
    left: 50%;
    transform: translateX(-50%) translateY(4px);
    min-width: 220px;
    max-width: 280px;
    background: #333;
    color: #fff;
    padding: 10px 14px 12px;
    border-radius: 6px;
    box-shadow: 0 6px 18px rgba(0,0,0,0.22);
    font-size: 0.9em;
    line-height: 1.45;
    text-align: left;
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.12s ease, transform 0.12s ease;
    z-index: 10;
  }}
  .badge-tooltip::after {{
    content: '';
    position: absolute;
    top: 100%;
    left: 50%;
    transform: translateX(-50%);
    border: 6px solid transparent;
    border-top-color: #333;
  }}
  .badge-tile:hover .badge-tooltip {{
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }}
  .badge-tooltip .tt-title {{
    font-weight: 700;
    font-size: 1em;
    margin-bottom: 1px;
  }}
  .badge-tooltip .tt-sub {{
    color: #bbb;
    font-size: 0.82em;
    margin-bottom: 7px;
  }}
  .badge-tooltip .tt-list {{
    list-style: none;
    padding: 0;
    margin: 0;
  }}
  .badge-tooltip .tt-list li {{
    padding: 2px 0;
  }}
  .badge-tooltip .tt-list li + li {{
    border-top: 1px solid rgba(255,255,255,0.08);
    margin-top: 3px;
    padding-top: 5px;
  }}
  .badge-tooltip .tt-note {{
    margin-top: 8px;
    padding-top: 7px;
    border-top: 1px solid rgba(255,255,255,0.12);
    color: #d0b98a;
    font-size: 0.82em;
    font-style: italic;
  }}
</style>
</head>
<body>

<h1>Session Report</h1>
<p class=""report-ts"">{Escape(reportTimestamp)}</p>

<h2>Player Info</h2>
<table>
  <tr><th>Player Name</th><td>{Escape(playerName)}</td></tr>
  <tr><th>Pet Name</th><td>{Escape(petName)}</td></tr>
  <tr><th>Session Length</th><td>{Escape(sessionLength)}</td></tr>
  <tr><th>Distance with pet</th><td>{Escape(distanceWithPetDisplay)}</td></tr>
</table>

<h2>Money Summary</h2>
<table>
  <tr><th>Starting Amount</th><td>${startingMoney:0.00}</td></tr>
  <tr><th>Ending Amount</th><td>${endingMoney:0.00}</td></tr>
  <tr><th>Net Change</th><td class=""net"">{netSign}${netChange:0.00}</td></tr>
  <tr><th>Highest Reached</th><td>${highestMoney:0.00}</td></tr>
  <tr><th>Lowest Reached</th><td>${lowestMoney:0.00}</td></tr>
</table>

<h2>Purchases</h2>
{purchasesHtml}

<h2>Feedings</h2>
{feedingsHtml}

<h2>Money Over Time</h2>
<div class=""chart-wrap"">
  <canvas id=""chart""></canvas>
</div>

<h2>Pet stats over time</h2>
<div class=""chart-wrap"">
  <canvas id=""chartPetStats""></canvas>
</div>

<h2>Badges</h2>
{badgesHtml}

<h2>NPC chat transcript</h2>
<div class=""chat-section"">
{playerChatLogHtml}
</div>

<script src=""https://cdn.jsdelivr.net/npm/chart.js@4""></script>
<script>
(function() {{
  var startHour = {startHourStr};
  var showDays = {showDays};
  var data = {pointsJs};
  var dataHealth = {healthJs};
  var dataHappiness = {happinessJs};
  var dataEnergy = {energyJs};
  var dataHunger = {hungerJs};

  function fmtHour(cumH) {{
    var hod = ((startHour + cumH) % 24 + 24) % 24;
    var h12 = hod % 12 === 0 ? 12 : Math.floor(hod) % 12;
    var p = Math.floor(hod) < 12 ? 'AM' : 'PM';
    return h12 + ' ' + p;
  }}

  function dayNum(cumH) {{
    return Math.floor((startHour + cumH) / 24) + 1;
  }}

  var maxCum = data.length > 0 ? data[data.length - 1].x : 0;

  var firstAligned = (3 - (startHour % 3)) % 3;
  if (firstAligned > maxCum) firstAligned = 0;

  var ctx = document.getElementById('chart').getContext('2d');
  new Chart(ctx, {{
    type: 'line',
    data: {{
      datasets: [{{
        data: data,
        fill: {{
          target: 'origin',
          above: 'rgba(80,160,90,0.06)',
          below: 'rgba(180,60,60,0.06)'
        }},
        tension: 0.2,
        pointRadius: 3.5,
        pointBackgroundColor: '#4a7c59',
        pointBorderColor: '#fff',
        pointBorderWidth: 1.5,
        pointHoverRadius: 6,
        pointHoverBackgroundColor: '#333',
        borderWidth: 2.5,
        segment: {{
          borderColor: function(c) {{
            var y0 = c.p0.parsed.y, y1 = c.p1.parsed.y;
            if (y1 > y0) return '#3a9d4a';
            if (y1 < y0) return '#c44';
            return '#999';
          }}
        }}
      }}]
    }},
    options: {{
      responsive: true,
      maintainAspectRatio: true,
      aspectRatio: 2.8,
      plugins: {{
        legend: {{ display: false }},
        tooltip: {{
          backgroundColor: '#333',
          titleFont: {{ size: 12 }},
          bodyFont: {{ size: 12 }},
          padding: 8,
          cornerRadius: 4,
          callbacks: {{
            title: function(items) {{
              if (!items.length) return '';
              return fmtHour(items[0].parsed.x) + (showDays ? ' — Day ' + dayNum(items[0].parsed.x) : '');
            }},
            label: function(item) {{
              return '  $' + item.parsed.y.toFixed(2);
            }}
          }}
        }}
      }},
      scales: {{
        x: {{
          type: 'linear',
          title: {{ display: false }},
          grid: {{
            color: 'rgba(0,0,0,0.06)',
            drawTicks: true,
            tickLength: 6
          }},
          border: {{ color: '#ccc' }},
          ticks: {{
            maxRotation: 0,
            autoSkip: false,
            color: '#666',
            font: {{ size: 11 }},
            callback: function(val) {{
              var hod = ((startHour + val) % 24 + 24) % 24;
              var onThree = Math.abs(hod - Math.round(hod / 3) * 3) < 0.01;
              if (!onThree) return '';
              var label = fmtHour(val);
              if (showDays) {{
                var atMidnight = Math.abs(hod) < 0.01 || Math.abs(hod - 24) < 0.01;
                var isFirst = Math.abs(val) < 0.01;
                if (atMidnight || isFirst) {{
                  return [label, 'Day ' + dayNum(val)];
                }}
              }}
              return label;
            }},
            stepSize: 1
          }},
          afterBuildTicks: function(axis) {{
            var ticks = [];
            for (var t = firstAligned; t <= maxCum + 0.5; t += 3) {{
              ticks.push({{ value: t }});
            }}
            axis.ticks = ticks;
          }}
        }},
        y: {{
          title: {{
            display: true,
            text: 'Money ($)',
            color: '#777',
            font: {{ size: 12 }}
          }},
          grid: {{
            color: 'rgba(0,0,0,0.06)'
          }},
          border: {{ color: '#ccc' }},
          ticks: {{
            color: '#666',
            font: {{ size: 11 }},
            callback: function(val) {{ return '$' + val; }}
          }},
          beginAtZero: true
        }}
      }}
    }}
  }});

  // PetStatsBarsController default colors (RGB 0-1 → hex) for parity with in-game bars.
  var colHealth = '#e64d59';
  var colHappiness = '#f2cc40';
  var colEnergy = '#4099f2';
  var colHunger = '#73cc66';

  var petStatsEl = document.getElementById('chartPetStats');
  if (petStatsEl && (dataHealth.length || dataHappiness.length || dataEnergy.length || dataHunger.length)) {{
    var petLine = function(series, label, hex) {{
      return {{
        label: label,
        data: series,
        borderColor: hex,
        backgroundColor: hex,
        tension: 0.2,
        fill: false,
        pointRadius: 3.5,
        pointBackgroundColor: hex,
        pointBorderColor: '#fff',
        pointBorderWidth: 1.5,
        pointHoverRadius: 6,
        borderWidth: 2.5
      }};
    }};
    new Chart(petStatsEl.getContext('2d'), {{
      type: 'line',
      data: {{
        datasets: [
          petLine(dataHealth, 'Health', colHealth),
          petLine(dataHappiness, 'Happiness', colHappiness),
          petLine(dataEnergy, 'Energy', colEnergy),
          petLine(dataHunger, 'Hunger', colHunger)
        ]
      }},
      options: {{
        responsive: true,
        maintainAspectRatio: true,
        aspectRatio: 2.4,
        interaction: {{ mode: 'index', intersect: false }},
        plugins: {{
          legend: {{
            display: true,
            position: 'bottom',
            labels: {{
              usePointStyle: true,
              pointStyle: 'rectRounded',
              padding: 14,
              font: {{ size: 12 }}
            }}
          }},
          tooltip: {{
            backgroundColor: '#333',
            titleFont: {{ size: 12 }},
            bodyFont: {{ size: 12 }},
            padding: 8,
            cornerRadius: 4,
            callbacks: {{
              title: function(items) {{
                if (!items.length) return '';
                return fmtHour(items[0].parsed.x) + (showDays ? ' — Day ' + dayNum(items[0].parsed.x) : '');
              }},
              label: function(item) {{
                return ' ' + item.dataset.label + ': ' + item.parsed.y.toFixed(1);
              }}
            }}
          }}
        }},
        scales: {{
          x: xScaleConfig(),
          y: {{
            title: {{
              display: true,
              text: 'Stat (0-100)',
              color: '#777',
              font: {{ size: 12 }}
            }},
            min: 0,
            max: 100,
            grid: {{ color: 'rgba(0,0,0,0.06)' }},
            border: {{ color: '#ccc' }},
            ticks: {{
              color: '#666',
              font: {{ size: 11 }}
            }}
          }}
        }}
      }}
    }});
  }}

  function xScaleConfig() {{
    return {{
      type: 'linear',
      title: {{ display: false }},
      grid: {{
        color: 'rgba(0,0,0,0.06)',
        drawTicks: true,
        tickLength: 6
      }},
      border: {{ color: '#ccc' }},
      ticks: {{
        maxRotation: 0,
        autoSkip: false,
        color: '#666',
        font: {{ size: 11 }},
        callback: function(val) {{
          var hod = ((startHour + val) % 24 + 24) % 24;
          var onThree = Math.abs(hod - Math.round(hod / 3) * 3) < 0.01;
          if (!onThree) return '';
          var label = fmtHour(val);
          if (showDays) {{
            var atMidnight = Math.abs(hod) < 0.01 || Math.abs(hod - 24) < 0.01;
            var isFirst = Math.abs(val) < 0.01;
            if (atMidnight || isFirst) {{
              return [label, 'Day ' + dayNum(val)];
            }}
          }}
          return label;
        }},
        stepSize: 1
      }},
      afterBuildTicks: function(axis) {{
        var ticks = [];
        for (var t = firstAligned; t <= maxCum + 0.5; t += 3) {{
          ticks.push({{ value: t }});
        }}
        axis.ticks = ticks;
      }}
    }};
  }}
}})();
</script>
</body>
</html>";
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
