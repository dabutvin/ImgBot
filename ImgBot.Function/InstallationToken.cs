﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace ImgBot.Function
{
    public class InstallationToken
    {
        public string Token { get; set; }

        public string ExpiresAt { get; set; }

        public static async Task<InstallationToken> GenerateAsync(InstallationTokenParameters input, StreamReader privateKeyReader)
        {
            var jwtPayload = new
            {
                iat = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
                exp = (int)(DateTime.UtcNow.AddMinutes(9) - new DateTime(1970, 1, 1)).TotalSeconds,
                iss = input.AppId,
            };

            var header = new { alg = "RS256", typ = "JWT" };
            var headerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header, Formatting.None));
            var payloadBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jwtPayload, Formatting.None));

            var segments = new List<string>
            {
                Base64UrlEncode(headerBytes),
                Base64UrlEncode(payloadBytes),
            };

            var stringToSign = string.Join(".", segments);
            var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            ISigner signer = SignerUtilities.GetSigner("SHA-256withRSA");
            AsymmetricCipherKeyPair keyPair;
            using (privateKeyReader)
            {
                keyPair = (AsymmetricCipherKeyPair)new PemReader(privateKeyReader).ReadObject();
            }

            signer.Init(true, keyPair.Private);
            signer.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
            var sigBytes = signer.GenerateSignature();
            segments.Add(Base64UrlEncode(sigBytes));

            var jwttoken = string.Join(".", segments);

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.machine-man-preview+json");
                http.DefaultRequestHeaders.Add("User-Agent", "ImgBot");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwttoken);

                var result = await http.PostAsync(input.AccessTokensUrl, null);

                var json = await result.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<InstallationToken>(json);
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }
    }
}
