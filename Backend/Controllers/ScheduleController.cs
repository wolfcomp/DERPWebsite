using DERPWebsite.Hubs;
using DERPWebsite.Middlewares;
using DERPWebsite.Models;
using DERPWebsite.Services;

namespace DERPWebsite.Controllers;

[ApiController]
[Route("/api/[controller]/")]
public class ScheduleController : ControllerBase
{
    private readonly Database _database;
    private readonly DiscordConnection _discord;
    private readonly IHubContext<MainHub> _hub;

    public ScheduleController(Database database, DiscordConnection discord, IHubContext<MainHub> hub)
    {
        _database = database;
        _discord = discord;
        _hub = hub;
    }

    private DateTimeOffset GetThisWeek() => TimeZoneInfo.ConvertTime((DateTimeOffset)DateTimeOffset.UtcNow.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"));

    private Tuple<DateTimeOffset, DateTimeOffset> GetWeek(bool nextWeek = false)
    {
        var first = GetThisWeek();
        first = first.DayOfWeek switch
        {
            DayOfWeek.Wednesday => first.AddDays(-1),
            DayOfWeek.Thursday => first.AddDays(-2),
            DayOfWeek.Friday => first.AddDays(-3),
            DayOfWeek.Saturday => first.AddDays(-4),
            DayOfWeek.Sunday => first.AddDays(-5),
            DayOfWeek.Monday => first.AddDays(-6),
            _ => first
        };
        first = new DateTimeOffset(first.Year, first.Month, first.Day, 0, 0, 0, first.Offset);
        return nextWeek ? Tuple.Create(first.AddDays(7), first.AddDays(14)) : new Tuple<DateTimeOffset, DateTimeOffset>(first, first.AddDays(7));
    }

    [HttpGet]
    [Route("week")]
    public async Task<IActionResult> GetWeekSchedule()
    {
        var (first, last) = GetWeek();
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last).Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }

    [HttpGet]
    [Route("nextweek")]
    public async Task<IActionResult> GetNextWeekSchedule()
    {
        var (first, last) = GetWeek(true);
        var schedules = await _database.Schedules.ToListAsync();
        return Ok(schedules.Where(t => t.At >= first && t.At < last).Select(t => ScheduleHttp.FromSchedule(t, _discord, _database)));
    }


    [HttpGet, ServiceFilter(typeof(AuthFilter))]
    [Route("all")]
    public Task<IActionResult> GetSchedule()
    {
        var scheduleHttps = _database.Database.SqlQueryRaw<ScheduleHttp>("""
                                                     select s."Id", s."Name", s."HostId", s."Duration", s."At", a."VisualName" as "HostName" from "Schedules" s
                                                     left outer join "AboutInfos" a on s."HostId" = a."Id"
                                                     """).ToList();
        var nulls = scheduleHttps.DistinctBy(t => t.HostId).Where(t => t.HostName is null).ToDictionary(t => t.HostId, t => t.HostName);
        foreach (var (id, _) in nulls)
        {
            try
            {
                nulls[id] = _discord.GetUserName(id);
            }
            catch
            {
                nulls[id] = "N/A";
            }
        }

        foreach (var t in scheduleHttps)
        {
            if (nulls.TryGetValue(t.HostId, out var value))
            {
                t.HostName = value;
            }
        }
        return Task.FromResult<IActionResult>(Ok(scheduleHttps));
    }

    [HttpPost, ServiceFilter(typeof(AuthFilter))]
    [Route("add")]
    public async Task<IActionResult> AddSchedule([FromBody] ScheduleHttp scheduleHttp)
    {
        var schedule = scheduleHttp.GetSchedule();
        var added = _database.Schedules.Add(schedule);
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleAdded", new { Schedule = ScheduleHttp.FromSchedule(added.Entity, _discord, _database), NextWeek = GetWeek(true).Item1 <= schedule.At });
        return Ok();
    }

    [HttpPut, ServiceFilter(typeof(AuthFilter))]
    [Route("update")]
    public async Task<IActionResult> UpdateSchedule([FromBody] ScheduleHttp schedule)
    {
        await _database.Schedules.Where(t => t.Id == schedule.Id).ExecuteUpdateAsync(prop =>
            prop
                .SetProperty(k => k.Name, schedule.Name)
                .SetProperty(k => k.HostId, schedule.HostId)
                .SetProperty(k => k.Duration, schedule.Duration)
                .SetProperty(k => k.At, schedule.At)
            );
        await _database.SaveChangesAsync();
        var scheduleReturn = await _database.Schedules.FirstAsync(t => t.Id == schedule.Id);
        await _hub.Clients.All.SendAsync("ScheduleUpdated", new { Schedule = ScheduleHttp.FromSchedule(scheduleReturn, _discord, _database), NextWeek = GetWeek(true).Item1 <= schedule.At });
        return Ok();
    }

    [HttpDelete, ServiceFilter(typeof(AuthFilter))]
    [Route("delete")]
    public async Task<IActionResult> DeleteSchedule([FromBody] Guid remove)
    {
        await _database.Schedules.Where(t => t.Id == remove).ExecuteDeleteAsync();
        await _database.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("ScheduleDeleted", remove);
        return Ok();
    }
}

public class ScheduleHttp(Guid? id, string name, ulong hostId, string? hostName, TimeSpan duration, DateTime at)
{
    public Guid? Id { get; set; } = id;
    public string Name { get; set; } = name;
    public ulong HostId { get; set; } = hostId;
    public string? HostName { get; set; } = hostName;
    public TimeSpan Duration { get; set; } = duration;
    public DateTime At { get; set; } = at;
    public static ScheduleHttp FromSchedule(Schedule schedule, DiscordConnection discord, Database database)
    {
        return new ScheduleHttp(schedule.Id, schedule.Name, schedule.HostId, database.GetUserName(discord, schedule.HostId), schedule.Duration, schedule.At);
    }
}

public static class ScheduleExtensions
{
    public static Schedule GetSchedule(this ScheduleHttp schedule)
    {
        return new Schedule(null, schedule.Name, schedule.HostId, schedule.Duration, schedule.At);
    }

    public static string GetUserName(this Database database, DiscordConnection discord, ulong id)
    {
        var aboutInfo = database.AboutInfos.FirstOrDefault(t => t.Id == id);
        string? name = null;
        if (aboutInfo is not null)
        {
            name = aboutInfo.VisualName;
        }
        return name ?? discord.GetUserName(id) ?? "N/A";
    }

    public static string? GetUserName(this DiscordConnection discord, ulong id)
    {
        var user = discord.Guild!.GetUser(id);
        return user?.DisplayName;
    }
}