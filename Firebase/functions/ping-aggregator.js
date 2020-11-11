/*
Copyright 2020 Google Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

'use strict';

const MillisInDay = 1000 * 60 * 60 * 24;
const TimezoneSuffixRegex = /\+|-\d\d:\d\d$/
const DateRegex = /^\d\d\d\d-\d\d-\d\d/;
const TzPartsRegex = /^(\+|-)(\d+):(\d+)$/;

function GetLocalISODate(dateInMillis, tzoffset) {
  const suffix = (tzoffset > 0 ? '-' : '+') + String(Math.floor(Math.abs(tzoffset) / 60)).padStart(2, '0') + ':' + String(Math.abs(tzoffset) % 60).padStart(2, '0');
  return (new Date(dateInMillis - tzoffset * 60000)).toISOString().slice(0, -1) + suffix;
}

exports.aggregatePings = function(data, interval) {
  let startDate = 0;
  let tzSuffix = '';
  let tzoffset = 0;
  let count = 0;
  let minTime = Number.MAX_SAFE_INTEGER;
  let maxTime = 0;
  let points = [];
  for (let key of Object.keys(data)) {
    let rawDate = data[key]['time'];
    if (startDate == 0) {
      tzSuffix = TimezoneSuffixRegex.exec(rawDate)[0];
      startDate = Date.parse(DateRegex.exec(rawDate)[0] + 'T00:00:00' + tzSuffix);
      let tzParts = TzPartsRegex.exec(tzSuffix);
      tzoffset = parseInt(tzParts[2]) * 60  + parseInt(tzParts[3]);
      if (tzParts[1] == '+') {
        tzoffset = -1 * tzoffset;
      }
    }
    const ping = data[key]['ping'];
    let time = Date.parse(data[key]['time']);
   
    points.push([ping, time]);
    maxTime = Math.max(maxTime, time);
    minTime = Math.min(minTime, time)
    count++;
  }
  points.sort(function(a, b){return a[1] - b[1]});

  let buckets = {}

  for (let point of points) {
    let delta = point[1] - startDate;
    let bucket = Math.round(Math.floor(delta / interval) * interval, 0);
    if (!(bucket in buckets)) {
      buckets[bucket] = {
        startTime : (GetLocalISODate(startDate + bucket, tzoffset)),
        endTime : GetLocalISODate(startDate + bucket + interval, tzoffset),
        totalPings : 0,
        totalFailures : 0,
        maxPing : 0,
        minPing : Number.MAX_SAFE_INTEGER,
        avgPing : 0
      }
    } else {
      buckets[bucket].totalPings++;
      const ping = point[0];
      if (ping == -1) {
        buckets[bucket].totalFailures++;
      } else {
        if (ping < buckets[bucket].minPing) {
          buckets[bucket].minPing = ping;
        } else if (ping > buckets[bucket].maxPing) {
          buckets[bucket].maxPing = ping;
        }
      }
      buckets[bucket].avgPing = ((buckets[bucket].avgPing * (buckets[bucket].totalPings - 1)) + ping) / buckets[bucket].totalPings;
    }
  }

  return buckets;
}
