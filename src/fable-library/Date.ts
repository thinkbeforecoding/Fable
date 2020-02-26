/**
 * DateTimeOffset functions.
 *
 * Note: Date instances are always DateObjects in local
 * timezone (because JS dates are all kinds of messed up).
 * A local date returns UTC epoc when `.getTime()` is called.
 *
 * Basically; invariant: date.getTime() always return UTC time.
 */

import { fromValue, Long, ticksToUnixEpochMilliseconds, unixEpochMillisecondsToTicks } from "./Long";
import { compareDates, DateKind, dateOffset, IDateTime, IDateTimeOffset, padWithZeros } from "./Util";

export const offsetRegex = /(?:Z|[+-](\d+):?([0-5]?\d)?)\s*$/;

export function dateOffsetToString(offset: number) {
  const isMinus = offset < 0;
  offset = Math.abs(offset);
  const hours = ~~(offset / 3600000);
  const minutes = (offset % 3600000) / 60000;
  return (isMinus ? "-" : "+") +
    padWithZeros(hours, 2) + ":" +
    padWithZeros(minutes, 2);
}

export function dateToHalfUTCString(date: IDateTime, half: "first" | "second") {
  const str = date.toISOString();
  return half === "first"
    ? str.substring(0, str.indexOf("T"))
    : str.substring(str.indexOf("T") + 1, str.length - 1);
}

function dateToISOString(d: IDateTime, utc: boolean) {
  if (utc) {
    return d.toISOString();
  } else {
    // JS Date is always local
    const printOffset = d.kind == null ? true : d.kind === DateKind.Local;
    return padWithZeros(d.getFullYear(), 4) + "-" +
      padWithZeros(d.getMonth() + 1, 2) + "-" +
      padWithZeros(d.getDate(), 2) + "T" +
      padWithZeros(d.getHours(), 2) + ":" +
      padWithZeros(d.getMinutes(), 2) + ":" +
      padWithZeros(d.getSeconds(), 2) + "." +
      padWithZeros(d.getMilliseconds(), 3) +
      (printOffset ? dateOffsetToString(d.getTimezoneOffset() * -60000) : "");
  }
}

function dateToISOStringWithOffset(dateWithOffset: Date, offset: number) {
  const str = dateWithOffset.toISOString();
  return str.substring(0, str.length - 1) + dateOffsetToString(offset);
}

function dateToStringWithCustomFormat(date: Date, format: string, utc: boolean) {
  /*
      //This comment from (MIT) corefx/src/Common/src/CoreLib/System/Globalization/DateTimeFormat.cs
      Customized format patterns:
        P.S. Format in the table below is the internal number format used to display the pattern.
        Patterns   Format      Description                           Example
        =========  ==========  ===================================== ========
            "h"     "0"         hour (12-hour clock)w/o leading zero  3
            "hh"    "00"        hour (12-hour clock)with leading zero 03
            "hh*"   "00"        hour (12-hour clock)with leading zero 03
            "H"     "0"         hour (24-hour clock)w/o leading zero  8
            "HH"    "00"        hour (24-hour clock)with leading zero 08
            "HH*"   "00"        hour (24-hour clock)                  08
            "m"     "0"         minute w/o leading zero
            "mm"    "00"        minute with leading zero
            "mm*"   "00"        minute with leading zero
            "s"     "0"         second w/o leading zero
            "ss"    "00"        second with leading zero
            "ss*"   "00"        second with leading zero
            "f"     "0"         second fraction (1 digit)
            "ff"    "00"        second fraction (2 digit)
            "fff"   "000"       second fraction (3 digit)
            "ffff"  "0000"      second fraction (4 digit)
            "fffff" "00000"         second fraction (5 digit)
            "ffffff"    "000000"    second fraction (6 digit)
            "fffffff"   "0000000"   second fraction (7 digit)
            "F"     "0"         second fraction (up to 1 digit)
            "FF"    "00"        second fraction (up to 2 digit)
            "FFF"   "000"       second fraction (up to 3 digit)
            "FFFF"  "0000"      second fraction (up to 4 digit)
            "FFFFF" "00000"         second fraction (up to 5 digit)
            "FFFFFF"    "000000"    second fraction (up to 6 digit)
            "FFFFFFF"   "0000000"   second fraction (up to 7 digit)
            "t"                 first character of AM/PM designator   A
            "tt"                AM/PM designator                      AM
            "tt*"               AM/PM designator                      PM
            "d"     "0"         day w/o leading zero                  1
            "dd"    "00"        day with leading zero                 01
            "ddd"               short weekday name (abbreviation)     Mon
            "dddd"              full weekday name                     Monday
            "dddd*"             full weekday name                     Monday
            "M"     "0"         month w/o leading zero                2
            "MM"    "00"        month with leading zero               02
            "MMM"               short month name (abbreviation)       Feb
            "MMMM"              full month name                       Febuary
            "MMMM*"             full month name                       Febuary
            "y"     "0"         two digit year (year % 100) w/o leading zero           0
            "yy"    "00"        two digit year (year % 100) with leading zero          00
            "yyy"   "D3"        year                                  2000
            "yyyy"  "D4"        year                                  2000
            "yyyyy" "D5"        year                                  2000
            ...
            "z"     "+0;-0"     timezone offset w/o leading zero      -8
            "zz"    "+00;-00"   timezone offset with leading zero     -08
            "zzz"      "+00;-00" for hour offset, "00" for minute offset  full timezone offset   -07:30
            "zzz*"  "+00;-00" for hour offset, "00" for minute offset   full timezone offset   -08:00
            "K"    -Local       "zzz", e.g. -08:00
                  -Utc         "'Z'", representing UTC
                  -Unspecified ""
                  -DateTimeOffset      "zzzzz" e.g -07:30:15
            "g*"                the current era name                  A.D.
            ":"                 time separator
            : -- DEPRECATED - Insert separator directly into pattern (eg: "H.mm.ss")
            "/"                 date separator
            /-- DEPRECATED - Insert separator directly into pattern (eg: "M-dd-yyyy")
            "'"                 quoted string                         'ABC' will insert ABC into the formatted string.
            '"'                 quoted string                         "ABC" will insert ABC into the formatted string.
            "%"                 used to quote a single pattern characters      E.g.The format character "%y" is to
              print two digit year.
            "\"                 escaped character                     E.g. '\d' insert the character 'd' into the
              format string.
            other characters    insert the character into the format string.
        Pre-defined format characters:
            (U) to indicate Universal time is used.
            (G) to indicate Gregorian calendar is used.
            Format              Description                             Real format
            =========           =================================       ======================
            "d"                 short date                              culture-specific
              10/31/1999
            "D"                 long data                               culture-specific
              Sunday, October 31, 1999
            "f"                 full date (long date + short time)      culture-specific
              Sunday, October 31, 1999 2:00 AM
            "F"                 full date (long date + long time)       culture-specific
              Sunday, October 31, 1999 2:00:00 AM
            "g"                 general date (short date + short time)  culture-specific
              10/31/1999 2:00 AM
            "G"                 general date (short date + long time)   culture-specific
              10/31/1999 2:00:00 AM
            "m"/"M"             Month/Day date                          culture-specific
              October 31
    (G)     "o"/"O"             Round Trip XML                          "yyyy-MM-ddTHH:mm:ss.fffffffK"
              1999-10-31 02:00:00.0000000Z
    (G)     "r"/"R"             RFC 1123 date,                          "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"
              Sun, 31 Oct 1999 10:00:00 GMT
    (G)     "s"                 Sortable format, based on ISO 8601.     "yyyy-MM-dd'T'HH:mm:ss"
              1999-10-31T02:00:00
                                                                        ('T' for local time)
            "t"                 short time                              culture-specific
              2:00 AM
            "T"                 long time                               culture-specific
              2:00:00 AM
    (G)     "u"                 Universal time with sortable format,    "yyyy'-'MM'-'dd HH':'mm':'ss'Z'"
              1999-10-31 10:00:00Z
                                based on ISO 8601.
    (U)     "U"                 Universal time with full                culture-specific
              Sunday, October 31, 1999 10:00:00 AM
                                (long date + long time) format
            "y"/"Y"             Year/Month day                          culture-specific
              October, 1999
    */
  switch (format) {
    case "d": // TODO localization
      format = "M/d/yyyy"; break;
    case "D": // TODO localization
      format = "dddd, MMMM d, yyyy"; break;
    case "f": // TODO localization
      format = "dddd, MMMM d, yyyy hh:mm tt"; break;
    case "F": // TODO localization
      format = "dddd, MMMM d, yyyy hh:mm:ss tt"; break;
    case "g": // TODO localization
      format = "MM/dd/yyyy hh:mm tt"; break;
    case "G": // TODO localization
      format = "MM/d/yyyy hh:mm:ss tt"; break;
    case "m": case "M": // TODO localization
      format = "MMMM d"; break;
    // case "o": case "O": //offset dates are handled by a different function (GMT)
    //   format = "yyyy-MM-ddTHH:mm:ss.fffffffK"; break;
    // case "r": case "R": //offset dates are handled by a different function (GMT)
    //  format =  "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"; break;
    // case "s": //offset dates are handled by a different function (GMT)
    //   format = "yyyy-MM-dd'T'HH:mm:ss"; break; // TODO single quoted things should not be replaced
    case "t": // TODO localization
      format = "HH:mm tt"; break;
    case "T": // TODO localization
      format = "HH:mm:ss tt"; break;
    // case "u": //offset dates are handled by a different function (GMT)
    //   format = "yyyy-mm-dd hh:mm:ssZ"; break;
    case "U": // TODO localization
      format = "dddd, MMMM d, yyyy h:m:s"; break;
  }

  return format.replace(/(\w)\1*/g, (match: string) => {
    let rep : ?string = null;
    switch (match.substring(0, 1)) {
      case "y":
        const y = utc ? date.getUTCFullYear() : date.getFullYear();
        rep = "" + (match.length < 4 ? y % 100 : y); 
        break;
      case "M":
        // TODO localization
        const months = [ "January",
          "February",
          "March",
          "April",
          "May",
          "June",
          "July",
          "August",
          "September",
          "October",
          "November",
          "December" ];

        // TODO localization
        const monthsShort = [ "Jan",
          "Feb",
          "Mar",
          "Apr",
          "May",
          "Jun",
          "Jul",
          "Aug",
          "Sep",
          "Oct",
          "Nov",
          "Dec"];
        const m = (utc ? date.getUTCMonth() : date.getMonth());
        if (match.length > 3) {
          rep = months[m];
        } else if (match.length === 3) {
          rep = monthsShort[m];
        } else {
          rep = "" + (m + 1);
        }
        break;
      case "d":
        // TODO localization
        const weekdays =  [ "Sunday",
          "Monday",
          "Tuesday",
          "Wednesday",
          "Thursday",
          "Friday",
          "Saturday" ];
        // TODO localization
        const weekdaysShort =  [ "Sun",
          "Mon",
          "Tue",
          "Wed",
          "Thu",
          "Fri",
          "Sat" ];
        if (match.length > 3) {
          rep = weekdays[date.getDay()];
        } else if (match.length === 3) {
          rep = weekdaysShort[date.getDay()];
        } else {
          rep = "" + (utc ? date.getUTCDate() : date.getDate());
        }
        break;
      case "H": 
        rep = "" + (utc ? date.getUTCHours() : date.getHours()); 
        break;
      case "h":
        const h = utc ? date.getUTCHours() : date.getHours();
        rep = "" + (h > 12 ? h % 12 : h);
        break;
      case "m": 
        rep = "" + (utc ? date.getUTCMinutes() : date.getMinutes());
        break;
      case "s": 
        rep = "" + (utc ? date.getUTCSeconds() : date.getSeconds());
        break;
      case "f": 
        rep = "" + (utc ? date.getUTCMilliseconds() : date.getMilliseconds());
        break;
      case "f": case "F":
        rep = padWithZeros((utc ? date.getUTCMilliseconds() : date.getMilliseconds()), 3);
        rep = rep.slice(0, match.length);
        break;
      case "z": 
        rep = "" + (utc ? 0 : date.getTimezoneOffset());
        break;
      case "t":
        const t = utc ? date.getUTCHours() : date.getHours();
        rep = t < 13 ? "AM" : "PM";
        rep = rep.slice(0, match.length);
        break;
    }
    rep
    if (rep == null) {
      return match;
    } else {
      return (rep.length < 2 && match.length > 1) ? "0" + rep : "" + rep;
    }
  });
}

function dateToStringWithOffset(date: IDateTimeOffset, format?: string) {
  const d = new Date(date.getTime() + (date.offset ?? 0));
  if (typeof format !== "string") {
    return d.toISOString().replace(/\.\d+/, "").replace(/[A-Z]|\.\d+/g, " ") + dateOffsetToString((date.offset ?? 0));
  } else if (format.length === 1) {
    switch (format) {
      case "D": case "d": return dateToHalfUTCString(d, "first");
      case "T": case "t": return dateToHalfUTCString(d, "second");
      case "O": case "o": return dateToISOStringWithOffset(d, (date.offset ?? 0));
      default: throw new Error("Unrecognized Date print format");
    }
  } else {
    return dateToStringWithCustomFormat(d, format, true);
  }
}

function dateToStringWithKind(date: IDateTime, format?: string) {
  const utc = date.kind === DateKind.UTC;
  if (typeof format !== "string") {
    return utc ? date.toUTCString() : date.toLocaleString();
  } else if (format.length === 1) {
    switch (format) {
      case "D": case "d":
        return utc ? dateToHalfUTCString(date, "first") : date.toLocaleDateString();
      case "T": case "t":
        return utc ? dateToHalfUTCString(date, "second") : date.toLocaleTimeString();
      case "O": case "o":
        return dateToISOString(date, utc);
      default:
        throw new Error("Unrecognized Date print format");
    }
  } else {
    return dateToStringWithCustomFormat(date, format, utc);
  }
}

export function toString(date: IDateTime | IDateTimeOffset, format?: string) {
  return (date as IDateTimeOffset).offset != null
    ? dateToStringWithOffset(date, format)
    : dateToStringWithKind(date, format);
}

export default function DateTime(value: number, kind?: DateKind) {
  const d = new Date(value) as IDateTime;
  d.kind = (kind == null ? DateKind.Unspecified : kind) | 0;
  return d;
}

export function fromTicks(ticks: number | Long, kind?: DateKind) {
  ticks = fromValue(ticks);
  kind = kind != null ? kind : DateKind.Unspecified;
  let date = DateTime(ticksToUnixEpochMilliseconds(ticks), kind);

  // Ticks are local to offset (in this case, either UTC or Local/Unknown).
  // If kind is anything but UTC, that means that the tick number was not
  // in utc, thus getTime() cannot return UTC, and needs to be shifted.
  if (kind !== DateKind.UTC) {
    date = DateTime(date.getTime() - dateOffset(date), kind);
  }

  return date;
}

export function fromDateTimeOffset(date: IDateTimeOffset, kind: DateKind) {
  switch (kind) {
    case DateKind.UTC: return DateTime(date.getTime(), DateKind.UTC);
    case DateKind.Local: return DateTime(date.getTime(), DateKind.Local);
    default:
      const d = DateTime(date.getTime() + (date.offset ?? 0), kind);
      return DateTime(d.getTime() - dateOffset(d), kind);
  }
}

export function getTicks(date: IDateTime | IDateTimeOffset) {
  return unixEpochMillisecondsToTicks(date.getTime(), dateOffset(date));
}

export function minValue() {
  // This is "0001-01-01T00:00:00.000Z", actual JS min value is -8640000000000000
  return DateTime(-62135596800000, DateKind.Unspecified);
}

export function maxValue() {
  // This is "9999-12-31T23:59:59.999Z", actual JS max value is 8640000000000000
  return DateTime(253402300799999, DateKind.Unspecified);
}

export function parseRaw(str: string) {
  let date = new Date(str);
  if (isNaN(date.getTime())) {
    // Try to check strings JS Date cannot parse (see #1045, #1422)
    // tslint:disable-next-line:max-line-length
    const m = /^\s*(\d+[^\w\s:]\d+[^\w\s:]\d+)?\s*(\d+:\d+(?::\d+(?:\.\d+)?)?)?\s*([AaPp][Mm])?\s*([+-]\d+(?::\d+)?)?\s*$/.exec(str);
    if (m != null) {
      let baseDate: Date;
      let timeInSeconds = 0;
      if (m[2] != null) {
        const timeParts = m[2].split(":");
        timeInSeconds =
          parseInt(timeParts[0], 10) * 3600 +
          parseInt(timeParts[1] || "0", 10) * 60 +
          parseFloat(timeParts[2] || "0");
        if (m[3] != null && m[3].toUpperCase() === "PM") {
          timeInSeconds += 720;
        }
      }
      if (m[4] != null) { // There's an offset, parse as UTC
        if (m[1] != null) {
          baseDate = new Date(m[1] + " UTC");
        } else {
          const d = new Date();
          baseDate = new Date(d.getUTCFullYear() + "/" + (d.getUTCMonth() + 1) + "/" + d.getUTCDate());
        }
        const offsetParts = m[4].substr(1).split(":");
        let offsetInMinutes = parseInt(offsetParts[0], 10) * 60 + parseInt(offsetParts[1] || "0", 10);
        if (m[4][0] === "+") {
          offsetInMinutes *= -1;
        }
        timeInSeconds += offsetInMinutes * 60;
      } else {
        if (m[1] != null) {
          baseDate = new Date(m[1]);
        } else {
          const d = new Date();
          baseDate = new Date(d.getFullYear() + "/" + (d.getMonth() + 1) + "/" + d.getDate());
        }
      }
      date = new Date(baseDate.getTime() + timeInSeconds * 1000);
      // correct for daylight savings time
      date = new Date(date.getTime() + (date.getTimezoneOffset() - baseDate.getTimezoneOffset()) * 60000);
    } else {
      throw new Error("The string is not a valid Date.");
    }
  }
  return date;
}

export function parse(str: string, detectUTC = false): IDateTime {
  const date = parseRaw(str);
  const offset = offsetRegex.exec(str);
  // .NET always parses DateTime as Local if there's offset info (even "Z")
  // Newtonsoft.Json uses UTC if the offset is "Z"
  const kind = offset != null
    ? (detectUTC && offset[0] === "Z" ? DateKind.UTC : DateKind.Local)
    : DateKind.Unspecified;
  return DateTime(date.getTime(), kind);
}

export function tryParse(v: string): [boolean, IDateTime] {
  try {
    // if value is null or whitespace, parsing fails
    if (v == null || v.trim() === "") {
      return [false, minValue()];
    }
    return [true, parse(v)];
  } catch (_err) {
    return [false, minValue()];
  }
}

export function create(
  year: number, month: number, day: number,
  h: number = 0, m: number = 0, s: number = 0,
  ms: number = 0, kind?: DateKind) {
  const dateValue = kind === DateKind.UTC
    ? Date.UTC(year, month - 1, day, h, m, s, ms)
    : new Date(year, month - 1, day, h, m, s, ms).getTime();
  if (isNaN(dateValue)) {
    throw new Error("The parameters describe an unrepresentable Date.");
  }
  const date = DateTime(dateValue, kind);
  if (year <= 99) {
    date.setFullYear(year, month - 1, day);
  }
  return date;
}

export function now() {
  return DateTime(Date.now(), DateKind.Local);
}

export function utcNow() {
  return DateTime(Date.now(), DateKind.UTC);
}

export function today() {
  return date(now());
}

export function isLeapYear(year: number) {
  return year % 4 === 0 && year % 100 !== 0 || year % 400 === 0;
}

export function daysInMonth(year: number, month: number) {
  return month === 2
    ? (isLeapYear(year) ? 29 : 28)
    : (month >= 8 ? (month % 2 === 0 ? 31 : 30) : (month % 2 === 0 ? 30 : 31));
}

export function toUniversalTime(date: IDateTime) {
  return date.kind === DateKind.UTC ? date : DateTime(date.getTime(), DateKind.UTC);
}

export function toLocalTime(date: IDateTime) {
  return date.kind === DateKind.Local ? date : DateTime(date.getTime(), DateKind.Local);
}

export function specifyKind(d: IDateTime, kind: DateKind) {
  return create(year(d), month(d), day(d), hour(d), minute(d), second(d), millisecond(d), kind);
}

export function timeOfDay(d: IDateTime) {
  return hour(d) * 3600000
    + minute(d) * 60000
    + second(d) * 1000
    + millisecond(d);
}

export function date(d: IDateTime) {
  return create(year(d), month(d), day(d), 0, 0, 0, 0, d.kind);
}

export function day(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCDate() : d.getDate();
}

export function hour(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCHours() : d.getHours();
}

export function millisecond(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCMilliseconds() : d.getMilliseconds();
}

export function minute(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCMinutes() : d.getMinutes();
}

export function month(d: IDateTime) {
  return (d.kind === DateKind.UTC ? d.getUTCMonth() : d.getMonth()) + 1;
}

export function second(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCSeconds() : d.getSeconds();
}

export function year(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCFullYear() : d.getFullYear();
}

export function dayOfWeek(d: IDateTime) {
  return d.kind === DateKind.UTC ? d.getUTCDay() : d.getDay();
}

export function dayOfYear(d: IDateTime) {
  const _year = year(d);
  const _month = month(d);
  let _day = day(d);
  for (let i = 1; i < _month; i++) {
    _day += daysInMonth(_year, i);
  }
  return _day;
}

export function add(d: IDateTime, ts: number) {
  const newDate = DateTime(d.getTime() + ts, d.kind);
  if (d.kind === DateKind.Local) {
    const oldTzOffset = d.getTimezoneOffset();
    const newTzOffset = newDate.getTimezoneOffset();
    return oldTzOffset !== newTzOffset
      ? DateTime(newDate.getTime() + (newTzOffset - oldTzOffset) * 60000, d.kind)
      : newDate;
  } else {
    return newDate;
  }
}

export function addDays(d: IDateTime, v: number) {
  return add(d, v * 86400000);
}

export function addHours(d: IDateTime, v: number) {
  return add(d, v * 3600000);
}

export function addMinutes(d: IDateTime, v: number) {
  return add(d, v * 60000);
}

export function addSeconds(d: IDateTime, v: number) {
  return add(d, v * 1000);
}

export function addMilliseconds(d: IDateTime, v: number) {
  return add(d, v);
}

export function addYears(d: IDateTime, v: number) {
  const newMonth = month(d);
  const newYear = year(d) + v;
  const _daysInMonth = daysInMonth(newYear, newMonth);
  const newDay = Math.min(_daysInMonth, day(d));
  return create(newYear, newMonth, newDay, hour(d), minute(d), second(d),
    millisecond(d), d.kind);
}

export function addMonths(d: IDateTime, v: number) {
  let newMonth = month(d) + v;
  let newMonth_ = 0;
  let yearOffset = 0;
  if (newMonth > 12) {
    newMonth_ = newMonth % 12;
    yearOffset = Math.floor(newMonth / 12);
    newMonth = newMonth_;
  } else if (newMonth < 1) {
    newMonth_ = 12 + newMonth % 12;
    yearOffset = Math.floor(newMonth / 12) + (newMonth_ === 12 ? -1 : 0);
    newMonth = newMonth_;
  }
  const newYear = year(d) + yearOffset;
  const _daysInMonth = daysInMonth(newYear, newMonth);
  const newDay = Math.min(_daysInMonth, day(d));
  return create(newYear, newMonth, newDay, hour(d), minute(d), second(d),
    millisecond(d), d.kind);
}

export function subtract(d: IDateTime, that: IDateTime | number) {
  return typeof that === "number" ? add(d, -that) : d.getTime() - that.getTime();
}

export function toLongDateString(d: IDateTime) {
  return d.toDateString();
}

export function toShortDateString(d: IDateTime) {
  return d.toLocaleDateString();
}

export function toLongTimeString(d: IDateTime) {
  return d.toLocaleTimeString();
}

export function toShortTimeString(d: IDateTime) {
  return d.toLocaleTimeString().replace(/:\d\d(?!:)/, "");
}

export function equals(d1: IDateTime, d2: IDateTime) {
  return d1.getTime() === d2.getTime();
}

export const compare = compareDates;
export const compareTo = compareDates;

export function op_Addition(x: IDateTime, y: number) {
  return add(x, y);
}

export function op_Subtraction(x: IDateTime, y: number | Date) {
  return subtract(x, y);
}

export function isDaylightSavingTime(x: IDateTime) {
  const jan = new Date(x.getFullYear(), 0, 1);
  const jul = new Date(x.getFullYear(), 6, 1);
  return isDST(jan.getTimezoneOffset(), jul.getTimezoneOffset(), x.getTimezoneOffset());
}

function isDST(janOffset: number, julOffset: number, tOffset: number) {
  return Math.min(janOffset, julOffset) === tOffset;
}
