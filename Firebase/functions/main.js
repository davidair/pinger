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

// Used for testing ping-aggregator
var fs = require('fs');
const pingAgregator = require('./ping-aggregator')

const MillisInDay = 1000 * 60 * 60 * 24;
const TenMinutesMillis = 1000 * 60 * 10;

function printBuckets(filename) {
  buckets = pingAgregator.aggregatePings(JSON.parse(fs.readFileSync(filename)));

  for (let startTime = 0; startTime < MillisInDay; startTime += TenMinutesMillis)  {
    if (startTime in buckets) {
      console.log(buckets[startTime]);
    }
  }
}

// printBuckets('_SOMEPATH__', TenMinutesMillis);

