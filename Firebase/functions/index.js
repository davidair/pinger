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

const functions = require('firebase-functions');
const admin = require('firebase-admin');
const pingAgregator = require('./ping-aggregator')

const TenMinutesMillis = 1000 * 60 * 10;
const OneMinuteMillis = 1000 * 60;

const DateRegex = /^\d\d\d\d-\d\d-\d\d$/g;

admin.initializeApp(functions.config().firebase);

// Process and aggregate logs
exports.processRawLogs = functions.https.onRequest((request, response) => {
   functions.logger.info('Processing raw logs', {structuredData: true});
   if (!request.query.date) {
    return response.send(500, {message: 'Error: date not specified'});
   }
   
   if (!request.query.date.match(DateRegex)) {
    return response.send(500, {message: 'Error: invalid date format'});
   }

   const date = Date.parse(request.query.date + 'T00:00:00Z');
   const now = Date.now();
   if (date > now && request.query.date != '2999-01-01') {
    return response.send(500, {message: 'Error: date should not be in the future'});
   }

   let interval = TenMinutesMillis;
   if (request.query.interval) {
    if (request.query.interval == '1') {
      interval = OneMinuteMillis;
    } else if (request.query.interval == '10') {
      interval = TenMinutesMillis;
    } else {
      return response.send(500, {message: 'Error: invalid interval specified'});
    }
   }

   return admin.database().ref('/raw_pings/' + request.query.date).once('value', (snapshot) => {
    var data = snapshot.val();
    if (!data) {
      return response.send(500, {message: 'Error: no data found for ' + request.query.date});
    }
    buckets = pingAgregator.aggregatePings(data, interval);
  
    admin.database().ref('/aggregated_pings/' + interval + '/' + request.query.date).set(buckets);
    return response.send(200, {message: 'Processed ' + Object.keys(buckets).length + ' bucket(s)'});
   });
 });
