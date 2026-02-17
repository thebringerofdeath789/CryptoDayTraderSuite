using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CryptoDayTraderSuite.Util
{
    public static class CoinbaseJwtUtil
    {
        private const uint BcryptEcdsaPrivateP256Magic = 0x32534345;

        public static string CreateJwt(string apiKeyName, string ecPrivateKeyPem, string method, string host, string requestPathAndQuery)
        {
            if (string.IsNullOrWhiteSpace(apiKeyName)) throw new InvalidOperationException("Coinbase API key name is required for advanced JWT auth.");
            if (string.IsNullOrWhiteSpace(ecPrivateKeyPem)) throw new InvalidOperationException("Coinbase EC private key PEM is required for advanced JWT auth.");
            if (string.IsNullOrWhiteSpace(method)) throw new InvalidOperationException("HTTP method is required for Coinbase JWT auth.");
            if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Request host is required for Coinbase JWT auth.");
            if (string.IsNullOrWhiteSpace(requestPathAndQuery)) requestPathAndQuery = "/";

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = now + 120;
            var nonce = GenerateNonce(16);
            var upperMethod = method.Trim().ToUpperInvariant();

            var header = new Dictionary<string, object>
            {
                { "alg", "ES256" },
                { "kid", apiKeyName },
                { "nonce", nonce },
                { "typ", "JWT" }
            };

            var payload = new Dictionary<string, object>
            {
                { "iss", "cdp" },
                { "sub", apiKeyName },
                { "nbf", now },
                { "exp", exp },
                { "uri", upperMethod + " " + host + requestPathAndQuery }
            };

            var headerJson = UtilCompat.JsonSerialize(header);
            var payloadJson = UtilCompat.JsonSerialize(payload);
            var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            var signingInput = headerB64 + "." + payloadB64;

            var signature = SignEs256(ecPrivateKeyPem, Encoding.ASCII.GetBytes(signingInput));
            var jose = ConvertSignatureToJose(signature, 32);
            var signatureB64 = Base64UrlEncode(jose);

            return signingInput + "." + signatureB64;
        }

        public static bool IsAdvancedCredentialShape(string apiKey, string apiSecret)
        {
            return CoinbaseCredentialNormalizer.LooksLikeApiKeyName(apiKey) && CoinbaseCredentialNormalizer.LooksLikePem(apiSecret);
        }

        private static byte[] SignEs256(string ecPrivateKeyPem, byte[] payload)
        {
            using (var key = ImportEcPrivateKey(ecPrivateKeyPem))
            using (var ecdsa = new ECDsaCng(key))
            {
                return ecdsa.SignData(payload, HashAlgorithmName.SHA256);
            }
        }

        private static CngKey ImportEcPrivateKey(string pem)
        {
            if (string.IsNullOrWhiteSpace(pem)) throw new InvalidOperationException("Coinbase EC private key is empty.");

            var normalized = CoinbaseCredentialNormalizer.NormalizePem(pem);
            byte[] der;

            if (TryExtractPemPayload(normalized, "-----BEGIN PRIVATE KEY-----", "-----END PRIVATE KEY-----", out der))
            {
                try
                {
                    return CngKey.Import(der, CngKeyBlobFormat.Pkcs8PrivateBlob);
                }
                catch
                {
                }
            }

            if (TryExtractPemPayload(normalized, "-----BEGIN EC PRIVATE KEY-----", "-----END EC PRIVATE KEY-----", out der))
            {
                try
                {
                    return CngKey.Import(der, CngKeyBlobFormat.Pkcs8PrivateBlob);
                }
                catch
                {
                }

                try
                {
                    var wrappedPkcs8 = WrapSec1AsPkcs8(der);
                    return CngKey.Import(wrappedPkcs8, CngKeyBlobFormat.Pkcs8PrivateBlob);
                }
                catch
                {
                }

                var sec1Blob = BuildEccPrivateBlobFromSec1Der(der);
                return CngKey.Import(sec1Blob, CngKeyBlobFormat.EccPrivateBlob);
            }

            throw new InvalidOperationException("Coinbase EC private key PEM format is invalid.");
        }

        private static bool TryExtractPemPayload(string normalizedPem, string markerStart, string markerEnd, out byte[] der)
        {
            der = null;
            if (string.IsNullOrWhiteSpace(normalizedPem)) return false;
            if (string.IsNullOrWhiteSpace(markerStart) || string.IsNullOrWhiteSpace(markerEnd)) return false;

            var start = normalizedPem.IndexOf(markerStart, StringComparison.OrdinalIgnoreCase);
            var end = normalizedPem.IndexOf(markerEnd, StringComparison.OrdinalIgnoreCase);
            if (start < 0 || end < 0 || end <= start)
            {
                return false;
            }

            var base64 = normalizedPem.Substring(start + markerStart.Length, end - (start + markerStart.Length));
            base64 = base64.Replace("\n", string.Empty).Replace("\r", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(base64))
            {
                return false;
            }

            try
            {
                der = Convert.FromBase64String(base64);
                return der != null && der.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] BuildEccPrivateBlobFromSec1Der(byte[] der)
        {
            if (der == null || der.Length == 0)
            {
                throw new InvalidOperationException("Coinbase EC private key PEM base64 payload is invalid.");
            }

            byte[] privateKey;
            byte[] x;
            byte[] y;
            ParseSec1EcPrivateKey(der, out privateKey, out x, out y);

            if (privateKey.Length != 32 || x.Length != 32 || y.Length != 32)
            {
                throw new InvalidOperationException("Coinbase EC private key must be P-256.");
            }

            var blob = new byte[8 + (32 * 3)];
            WriteUInt32LE(blob, 0, BcryptEcdsaPrivateP256Magic);
            WriteUInt32LE(blob, 4, 32);
            Buffer.BlockCopy(x, 0, blob, 8, 32);
            Buffer.BlockCopy(y, 0, blob, 40, 32);
            Buffer.BlockCopy(privateKey, 0, blob, 72, 32);
            return blob;
        }

        private static byte[] WrapSec1AsPkcs8(byte[] sec1Der)
        {
            if (sec1Der == null || sec1Der.Length == 0)
            {
                throw new InvalidOperationException("SEC1 EC key payload is empty.");
            }

            var version = new byte[] { 0x02, 0x01, 0x00 };
            var algorithmIdentifier = new byte[]
            {
                0x30, 0x13,
                0x06, 0x07, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01,
                0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07
            };

            var privateKeyOctet = EncodeOctetString(sec1Der);
            var body = Concat(version, algorithmIdentifier, privateKeyOctet);
            return EncodeSequence(body);
        }

        private static byte[] EncodeSequence(byte[] body)
        {
            if (body == null) body = new byte[0];
            return Concat(new byte[] { 0x30 }, EncodeDerLength(body.Length), body);
        }

        private static byte[] EncodeOctetString(byte[] payload)
        {
            if (payload == null) payload = new byte[0];
            return Concat(new byte[] { 0x04 }, EncodeDerLength(payload.Length), payload);
        }

        private static byte[] EncodeDerLength(int length)
        {
            if (length < 0) throw new InvalidOperationException("Negative DER length.");
            if (length < 128)
            {
                return new[] { (byte)length };
            }

            var bytes = new List<byte>();
            var value = length;
            while (value > 0)
            {
                bytes.Insert(0, (byte)(value & 0xFF));
                value >>= 8;
            }

            if (bytes.Count > 4)
            {
                throw new InvalidOperationException("DER length too large.");
            }

            var result = new byte[1 + bytes.Count];
            result[0] = (byte)(0x80 | bytes.Count);
            for (int i = 0; i < bytes.Count; i++)
            {
                result[i + 1] = bytes[i];
            }

            return result;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var total = 0;
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] != null) total += parts[i].Length;
                }
            }

            var output = new byte[total];
            var offset = 0;
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part == null || part.Length == 0) continue;
                    Buffer.BlockCopy(part, 0, output, offset, part.Length);
                    offset += part.Length;
                }
            }

            return output;
        }

        private static void ParseSec1EcPrivateKey(byte[] der, out byte[] privateKey, out byte[] x, out byte[] y)
        {
            privateKey = new byte[0];
            x = new byte[0];
            y = new byte[0];

            var i = 0;
            ReadTag(der, ref i, 0x30);
            var seqLen = ReadLength(der, ref i);
            var seqEnd = i + seqLen;

            ReadTag(der, ref i, 0x02);
            var versionLen = ReadLength(der, ref i);
            i += versionLen;

            ReadTag(der, ref i, 0x04);
            var privLen = ReadLength(der, ref i);
            privateKey = LeftPad(ReadBytes(der, ref i, privLen), 32);

            while (i < seqEnd)
            {
                var tag = der[i++];
                var len = ReadLength(der, ref i);
                var contentStart = i;

                if (tag == 0xA1)
                {
                    var j = contentStart;
                    ReadTag(der, ref j, 0x03);
                    var bitLen = ReadLength(der, ref j);
                    if (bitLen > 0)
                    {
                        var unusedBits = der[j++];
                        if (unusedBits != 0) throw new InvalidOperationException("Unsupported EC public key bit-string padding.");
                        var point = ReadBytes(der, ref j, bitLen - 1);
                        if (point.Length == 65 && point[0] == 0x04)
                        {
                            x = ReadSlice(point, 1, 32);
                            y = ReadSlice(point, 33, 32);
                        }
                    }
                }

                i = contentStart + len;
            }

            if (x.Length == 0 || y.Length == 0)
            {
                throw new InvalidOperationException("Coinbase EC private key does not include an uncompressed public key point.");
            }
        }

        private static byte[] ConvertSignatureToJose(byte[] signature, int partSize)
        {
            if (signature == null || signature.Length == 0)
            {
                throw new InvalidOperationException("ECDSA signature is empty.");
            }

            var expectedRawLength = partSize * 2;
            if (signature.Length == expectedRawLength && signature[0] != 0x30)
            {
                return signature;
            }

            return ConvertDerSignatureToJose(signature, partSize);
        }

        private static byte[] ConvertDerSignatureToJose(byte[] der, int partSize)
        {
            var i = 0;
            ReadTag(der, ref i, 0x30);
            ReadLength(der, ref i);

            ReadTag(der, ref i, 0x02);
            var rLen = ReadLength(der, ref i);
            var r = ReadBytes(der, ref i, rLen);

            ReadTag(der, ref i, 0x02);
            var sLen = ReadLength(der, ref i);
            var s = ReadBytes(der, ref i, sLen);

            var output = new byte[partSize * 2];
            var rNorm = NormalizeUnsignedInteger(r, partSize);
            var sNorm = NormalizeUnsignedInteger(s, partSize);
            Buffer.BlockCopy(rNorm, 0, output, 0, partSize);
            Buffer.BlockCopy(sNorm, 0, output, partSize, partSize);
            return output;
        }

        private static byte[] NormalizeUnsignedInteger(byte[] raw, int size)
        {
            if (raw == null) return new byte[size];
            var offset = 0;
            while (offset < raw.Length - 1 && raw[offset] == 0x00) offset++;
            var length = raw.Length - offset;
            if (length > size) throw new InvalidOperationException("ECDSA signature component is larger than expected size.");

            var result = new byte[size];
            Buffer.BlockCopy(raw, offset, result, size - length, length);
            return result;
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string GenerateNonce(int bytes)
        {
            var buffer = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            var sb = new StringBuilder(bytes * 2);
            for (int i = 0; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static void ReadTag(byte[] data, ref int offset, byte expectedTag)
        {
            if (offset >= data.Length || data[offset] != expectedTag)
            {
                throw new InvalidOperationException("Unexpected ASN.1 tag while parsing Coinbase EC key.");
            }

            offset++;
        }

        private static int ReadLength(byte[] data, ref int offset)
        {
            if (offset >= data.Length) throw new InvalidOperationException("Invalid ASN.1 length.");
            var first = data[offset++];
            if ((first & 0x80) == 0)
            {
                return first;
            }

            var count = first & 0x7F;
            if (count <= 0 || count > 4 || offset + count > data.Length)
            {
                throw new InvalidOperationException("Unsupported ASN.1 length encoding.");
            }

            var len = 0;
            for (int j = 0; j < count; j++)
            {
                len = (len << 8) | data[offset++];
            }

            return len;
        }

        private static byte[] ReadBytes(byte[] data, ref int offset, int length)
        {
            if (length < 0 || offset + length > data.Length) throw new InvalidOperationException("Unexpected end of ASN.1 payload.");
            var result = new byte[length];
            Buffer.BlockCopy(data, offset, result, 0, length);
            offset += length;
            return result;
        }

        private static byte[] ReadSlice(byte[] data, int index, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(data, index, result, 0, length);
            return result;
        }

        private static byte[] LeftPad(byte[] value, int length)
        {
            if (value == null) return new byte[length];
            if (value.Length == length) return value;
            if (value.Length > length)
            {
                var cropped = new byte[length];
                Buffer.BlockCopy(value, value.Length - length, cropped, 0, length);
                return cropped;
            }

            var padded = new byte[length];
            Buffer.BlockCopy(value, 0, padded, length - value.Length, value.Length);
            return padded;
        }

        private static void WriteUInt32LE(byte[] target, int offset, uint value)
        {
            target[offset] = (byte)(value & 0xFF);
            target[offset + 1] = (byte)((value >> 8) & 0xFF);
            target[offset + 2] = (byte)((value >> 16) & 0xFF);
            target[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }
}