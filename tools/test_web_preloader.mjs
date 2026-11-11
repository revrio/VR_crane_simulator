import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {gzipSync} from 'node:zlib';
import {createHash, webcrypto} from 'node:crypto';
globalThis.crypto ??= webcrypto;
const source = await readFile(new URL('../Assets/WebGLTemplates/DownloadFirst/preloader.js', import.meta.url), 'utf8');
const {downloadAssets} = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
function fixture(role, text) {
  const body = gzipSync(text);
  return {body, file:{role, path:`Build/${role}.bin`, bytes:body.length,
    sha256:createHash('sha256').update(body).digest('hex'), encoding:'gzip', type:'application/octet-stream'}};
}

test('download barrier stays pending until every asset is complete and decompressed', async () => {
  const a = fixture('code','wasm bytes'), b = fixture('data','scene bytes');
  let release;
  const gate = new Promise(resolve => { release = resolve; });
  let settled = false;
  const progress = [];
  const result = downloadAssets({files:[a.file,b.file]}, {fetchFile:async url => {
    if (url.endsWith('data.bin')) await gate;
    return new Response(url.endsWith('code.bin') ? a.body : b.body);
  }, onProgress:f => progress.push(f)}).then(value => { settled = true; return value; });
  await new Promise(resolve => setTimeout(resolve, 30));
  assert.equal(settled, false);
  release();
  const assets = await result;
  assert.equal(await assets.get('code').text(),'wasm bytes');
  assert.equal(await assets.get('data').text(),'scene bytes');
  assert.equal(progress.at(-1),1);
});

test('a failed or truncated download never releases the barrier', async () => {
  const a = fixture('data','scene bytes');
  await assert.rejects(downloadAssets({files:[a.file]}, {fetchFile:async () => new Response('missing',{status:404})}), /HTTP 404/);
  await assert.rejects(downloadAssets({files:[a.file]}, {fetchFile:async () => new Response(a.body.subarray(1))}), /Incomplete/);
});

test('corrupt cached files are rejected and evicted', async () => {
  const a = fixture('data','scene bytes');
  let evicted = false;
  const bad = Buffer.from(a.body); bad[10] ^= 1;
  await assert.rejects(downloadAssets({files:[a.file]}, {cache:{match:async () => new Response(bad), delete:async () => {evicted=true;}}}), /verification/);
  assert.equal(evicted,true);
});

test('verified cache avoids network and cache quota errors do not prevent startup', async () => {
  const a = fixture('data','scene bytes');
  let network = 0;
  const fetchFile = async () => { network++; return new Response(a.body); };
  const cached = await downloadAssets({files:[a.file]}, {fetchFile,cache:{match:async () => new Response(a.body)}});
  assert.equal(await cached.get('data').text(),'scene bytes');
  assert.equal(network,0);
  const fresh = await downloadAssets({files:[a.file]}, {fetchFile,cache:{match:async () => null,put:async () => {throw Error('quota');}}});
  assert.equal(await fresh.get('data').text(),'scene bytes');
  assert.equal(network,1);
});
