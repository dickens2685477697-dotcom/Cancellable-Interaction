/* Independent Node.js cross-check of a closed Python-analysis snapshot.
 * Inputs are read only. No hardware-camera rate is inferred.
 */
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');

function* records(text) {
  let fields = [], field = '', quoted = false;
  text = text.replace(/^\uFEFF/, '');
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (quoted) {
      if (ch === '"' && text[i + 1] === '"') { field += '"'; i++; }
      else if (ch === '"') quoted = false;
      else field += ch;
    } else if (ch === '"' && field === '') quoted = true;
    else if (ch === ',') { fields.push(field); field = ''; }
    else if (ch === '\r' || ch === '\n') {
      if (ch === '\r' && text[i + 1] === '\n') i++;
      fields.push(field); yield fields; fields = []; field = '';
    } else field += ch;
  }
  assert(!quoted, 'Unclosed quoted CSV field');
  if (field !== '' || fields.length) { fields.push(field); yield fields; }
}

function* csv(file) {
  let headers;
  for (const fields of records(fs.readFileSync(file, 'utf8'))) {
    if (!headers) { headers = fields; continue; }
    assert.equal(fields.length, headers.length, `Malformed CSV row: ${file}`);
    yield Object.fromEntries(headers.map((key, i) => [key, fields[i]]));
  }
}

const yes = value => ['true', '1'].includes(String(value).toLowerCase());
const near = (a, b, label) => assert(Math.abs(a - b) < 1e-7, `${label}: ${a} != ${b}`);
const rate = times => (times.length - 1) / (times.at(-1) - times[0]);
const intervals = times => times.slice(1).map((t, i) => t - times[i]);
function stats(values) {
  const a = values.slice().sort((x, y) => x - y);
  const q = fraction => {
    const p = (a.length - 1) * fraction, i = Math.floor(p), j = Math.min(i + 1, a.length - 1);
    return (a[i] + (a[j] - a[i]) * (p - i)) * 1000;
  };
  return { count: a.length, min_ms: a[0] * 1000, p50_ms: q(.5), p95_ms: q(.95), p99_ms: q(.99), max_ms: a.at(-1) * 1000 };
}
function checkStats(actual, expected, label) {
  for (const key of Object.keys(actual)) near(actual[key], expected[key], `${label}.${key}`);
}

// CSV parser checks include quoting, multiline fields, BOM and a truncated row.
assert.deepEqual([...records('\uFEFFa,b\r\n"x,y","say ""hi"""\r\n')], [['a', 'b'], ['x,y', 'say "hi"']]);
assert.deepEqual([...records('a,b\n"x\ny",z')], [['a', 'b'], ['x\ny', 'z']]);
assert.throws(() => [...records('a,b\n"unfinished')]);

const folder = path.resolve(process.argv[2]), prefix = path.basename(folder);
const readJson = name => JSON.parse(fs.readFileSync(path.join(folder, name), 'utf8').replace(/^\uFEFF/, ''));
const report = readJson('formal-summary.json'), full = readJson('baseline-analysis.json');
const common = [...csv(path.join(folder, `${prefix}-common-motion.csv`))].filter(r => yes(r.experimentStarted) && yes(r.trackingValid));
assert.equal(report.consumer_groups.length, 1, 'This verification expects one formal consumer group');
const expected = report.consumer_groups[0], start = +common[0].realtime, stop = +common.at(-1).realtime;
assert.equal(common.length, expected.rows);
assert(common.every(r => r.sourceInstanceId === expected.ownerInstanceId && r.sampleSourceId === expected.sourceId && r.trackingEpoch === expected.trackingEpoch));
near(stop - start, expected.span_seconds, 'window duration');
near(rate(common.map(r => +r.realtime)), expected.callback_hz, 'consumer rate');
near((+common.at(-1).frame - +common[0].frame) / (stop - start), expected.estimated_application_fps, 'application FPS');

const sdk = new Map(), providers = new Map(), sdkWindow = [], providerWindow = [], counts = { observedSdkReadCount: 0, providerUpdateCount: 0, poseReadCount: 0 };
let endCounts;
for (const row of csv(path.join(folder, `${prefix}-hand-updates.csv`))) {
  const d = JSON.parse(row.detailsJson);
  if (row.event === 'sdk_read') {
    counts.observedSdkReadCount++;
    if (yes(d.readSucceeded)) {
      const value = { row, time: +d.readCompletedRealtime };
      sdk.set(`${row.sdkHandInstanceId}:${row.sdkReadSequence}`, value);
      if (yes(d.tracked) && value.time >= start && value.time <= stop) sdkWindow.push(value);
    }
  } else if (row.event === 'provider_update') {
    counts.providerUpdateCount++;
    const value = { row, d, time: +d.providerReceivedRealtime };
    providers.set(`${row.sourceId}:${d.sharedSampleSequence}`, value);
    if (['Left', 'Right'].includes(row.handedness) && value.time >= start && value.time <= stop) providerWindow.push(value);
  } else if (row.event === 'session_end') endCounts = d;
}
const reads = new Map();
for (const row of csv(path.join(folder, `${prefix}-hand-reads.csv`))) {
  counts.poseReadCount++;
  if (row.joint === 'IndexTip' && ['S2AccMode', 'S2Obstacles'].includes(row.ownerType)) {
    const key = [row.ownerInstanceId, row.sourceId, row.sharedSampleSequence, row.fixedTime].join(':');
    assert(!reads.has(key), 'Ambiguous shared consumption key'); reads.set(key, row);
  }
}
assert.deepEqual(counts, full.observed_counts);
for (const [key, count] of Object.entries(counts)) assert.equal(count, endCounts[key]);
assert.equal(sdkWindow.length, report.sdk_streams[0].tracked_reads);
near(rate(sdkWindow.map(s => s.time)), report.sdk_streams[0].observed_read_hz, 'SDK rate');
checkStats(stats(intervals(sdkWindow.map(s => s.time))), report.sdk_streams[0].observed_read_intervals, 'SDK intervals');
let equalStamps = 0;
for (let i = 0; i < sdkWindow.length; i++) {
  assert.equal(sdkWindow[i].row.sdkSampleTimeStamp, sdkWindow[i].row.sdkRequestedTimeStamp);
  if (i && sdkWindow[i].row.sdkSampleTimeStamp === sdkWindow[i - 1].row.sdkSampleTimeStamp) equalStamps++;
}
assert.equal(equalStamps, report.sdk_streams[0].equal_timestamp_pairs);
assert.equal(providerWindow.length, report.mrtk_hand_events_in_window);
const sdkToPublications = new Map();
for (const p of providerWindow) {
  assert.equal(p.d.matchStatus, 'unique_oculus_skeleton_pose_match');
  const key = `${p.row.sdkHandInstanceId}:${p.row.sdkReadSequence}`;
  if (!sdkToPublications.has(key)) sdkToPublications.set(key, []);
  sdkToPublications.get(key).push(p);
}
assert.equal(sdkToPublications.size, report.distinct_applied_sdk_states_referenced);
for (const pair of sdkToPublications.values()) {
  assert.equal(pair.length, 2); assert.notEqual(pair[0].row.sourceId, pair[1].row.sourceId);
  assert.equal(pair[0].row.frame, pair[1].row.frame); assert.equal(pair[0].row.handedness, pair[1].row.handedness);
}
for (const source of report.mrtk_source_streams) {
  const p = providerWindow.filter(p => p.row.sourceId === source.sourceId);
  assert.equal(p.length, source.events); near(rate(p.map(p => p.time)), source.observed_hz, 'source rate');
}

const ages = [[], [], []], repeatIntervals = [], status = {}, acceptedIds = new Set();
let repeated = 0, accepted = 0;
for (let i = 0; i < common.length; i++) {
  const observation = common[i], p = providers.get(`${observation.sampleSourceId}:${observation.sampleSequence}`);
  const r = reads.get([observation.sourceInstanceId, observation.sampleSourceId, observation.sampleSequence, observation.fixedTime].join(':'));
  assert(p && r, 'Missing exact MRTK/read linkage');
  assert.equal(p.d.matchStatus, 'unique_oculus_skeleton_pose_match');
  const s = sdk.get(`${p.row.sdkHandInstanceId}:${p.row.sdkReadSequence}`);
  assert(s, 'Missing candidate SDK linkage');
  assert.equal(+p.row.frame - +s.row.frame, 1); assert.equal(+r.frame - +p.row.frame, 1);
  ages[0].push(p.time - s.time); ages[1].push(+r.realtime - p.time); ages[2].push(+r.realtime - s.time);
  status[observation.sampleStatus] = (status[observation.sampleStatus] || 0) + 1;
  if (yes(observation.sampleAccepted)) { accepted++; acceptedIds.add(p.row.providerUpdateId); }
  if (observation.sampleStatus === 'duplicate_or_out_of_order') {
    repeated++;
    const previous = common[i - 1];
    for (const key of ['frame', 'sampleSequence', 'sampleTimestamp']) assert.equal(observation[key], previous[key]);
    assert(!yes(observation.sampleAccepted) && !yes(observation.selected));
    assert(Math.abs((+observation.fixedTime - +previous.fixedTime) * 1000 - 20) < 1e-4, 'repeat logical timestep');
    repeatIntervals.push(+observation.realtime - +previous.realtime);
  }
}
assert.deepEqual(status, expected.statuses);
assert.equal(accepted, expected.accepted_samples_including_warmup);
assert.equal(repeated, report.duplicate_evidence_rows);
near((accepted - 1) / (stop - start), expected.accepted_observation_rate_hz, 'accepted observation rate');
near(repeated / common.length, expected.repeated_fraction, 'repeated read fraction');
checkStats(stats(common.filter(r => yes(r.sampleAccepted) && yes(r.hasVelocity)).map(r => +r.acceptedMotionDtSeconds)), expected.accepted_sample_interval_ms, 'accepted sample dt');
checkStats(stats(repeatIntervals), report.repeat_record_intervals, 'repeat intervals');
for (const [index, key] of ['sdk_read_return_to_mrtk_receive', 'mrtk_receive_to_consumer_observation', 'sdk_read_return_to_consumer_observation'].entries()) checkStats(stats(ages[index]), expected[key], key);
checkStats(stats(intervals(common.map(r => +r.realtime))), expected.callback_record_intervals, 'callback intervals');
const usedInWindow = providerWindow.filter(p => p.row.sourceId === expected.sourceId && acceptedIds.has(p.row.providerUpdateId)).length;
assert.equal(usedInWindow, expected.of_those_events_accepted_by_this_consumer);
const files = readJson('snapshot-manifest.json').files.map(item => {
  const digest = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
  assert.equal(digest(item.snapshot), item.sha256); assert.equal(digest(item.source), item.sha256);
  return { name: path.basename(item.snapshot), snapshot_and_source_sha256_match: true };
});
const verification = { independently_verified_with: process.version, parser: 'Independent CSV reader and JSON.parse in Node.js',
  full_session_counts_reproduced: true, formal_counts_rates_and_stage_ages_reproduced: true,
  all_six_source_and_snapshot_hashes_match: true, raw_inputs_unchanged: true,
  hardware_first_capture_proven: false, counts, formal_rows: common.length, repeated_reads: repeated, files };
fs.writeFileSync(path.join(folder, 'verification.json'), JSON.stringify(verification, null, 2) + '\n');
process.stdout.write(JSON.stringify(verification, null, 2) + '\n');
