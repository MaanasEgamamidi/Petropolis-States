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
        IReadOnlyList<SessionChatLog.Entry> playerChatLog)
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
        string html = BuildHtml(
            reportTimestamp, playerName, petName, sessionLength, distanceStr,
            startingMoney, endingMoney, netChange, highestMoney, lowestMoney,
            pointsJs, healthJs, happinessJs, energyJs, hungerJs, startHour, totalGameHours,
            chatLogHtml);

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
                sb.Append($"    <div class=\"chat-line chat-you\"><strong>You</strong>: {Escape(e.Message)}</div>\n");
            else
                sb.Append($"    <div class=\"chat-line chat-npc\"><strong>{Escape(partner)}</strong>: {Escape(e.Message)}</div>\n");
        }

        if (prevPartner != null)
            sb.Append("  </div>\n");
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
        string playerChatLogHtml)
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
  .chat-transcript-empty {{
    color: #666;
    font-size: 0.95em;
    margin: 0;
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

<h2>Money Over Time</h2>
<div class=""chart-wrap"">
  <canvas id=""chart""></canvas>
</div>

<h2>Pet stats over time</h2>
<div class=""chart-wrap"">
  <canvas id=""chartPetStats""></canvas>
</div>

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
