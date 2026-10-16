# Connect dhara.aftanfaiz.in from Hostinger

The public site is deployed at https://dhara-aftanfaiz-in.ardentcoin17.chatgpt.site. Project title: dhara.aftanfaiz.in. Public access is enabled. The custom domain is registered with the host but remains pending DNS verification.

1. On your other computer, sign in to https://hpanel.hostinger.com.
2. Open Domains, select aftanfaiz.in, and open DNS / Nameservers > DNS records. Depending on the dashboard layout, use Domains > Domain portfolio > Manage first.
3. Add a CNAME record. Name: dhara. Points to: custom-domains.chatgpt.site.. Leave TTL at its default.
4. Add a TXT record. Name: _openai-site-verification.dhara. TXT value: openai-site-verification=W1Tbg3Xv--3BLSI6pV_Nbf3VZFxe5yEV_Kex36T7HC4. Leave TTL at its default.
5. Add another TXT record. Name: _cf-custom-hostname.dhara. TXT value: f7565130-7419-45e7-abc3-78ede399ae97. Leave TTL at its default.
6. Save each record. These short Name values assume Hostinger appends aftanfaiz.in. The full names must be dhara.aftanfaiz.in, _openai-site-verification.dhara.aftanfaiz.in and _cf-custom-hostname.dhara.aftanfaiz.in respectively. Do not append the root domain twice.
7. Leave your root-domain, www, mail, MX and nameserver records unchanged. If an existing A/AAAA/CNAME record for the exact name dhara conflicts, inspect that exact subdomain before replacing it.
8. Allow DNS propagation and certificate validation, then open https://dhara.aftanfaiz.in. The public fallback URL works while this is pending. If the custom hostname is still pending after DNS has propagated, ask for a custom-domain status refresh.

No Hostinger File Manager upload or new hosting plan is needed for this route. The website is already hosted; Hostinger supplies the DNS records.

Current release: public project/download page with the verified Windows application. Browser playback is not published. Unity Web Build Support is missing on the local machine; automated installation returned ELEVATION_FAILED. Install Web Build Support for Unity 6000.3.26f1 in Unity Hub to enable the next browser build. The prepared editor menu is DHARA > Build public Web application; its build output is Builds/Web-Dhara. That future browser build must still be tested and published.

Alternative if you choose to host the page on an existing Hostinger web hosting plan instead: Builds/DHARA-Hostinger-Upload.zip contains index.html, style.css, download.js, city.png and release assets at the archive root. Create a separate Custom PHP/HTML website for dhara.aftanfaiz.in and extract those files into that website's own document root. Do not use the CNAME route above at the same time; a Hostinger-hosted site uses the hosting IP/records shown by Hostinger.

Verified: local browser downloaded the complete Windows ZIP and its SHA-256 matched 8410AE30B30C57EF0386DB6BF541344833311BA2D93FA203D1241B88E890DB94. Public home page and download assets returned HTTP 200. The Web build entry point compiled and the 49 Edit Mode tests passed. Public source is a separate Sites repository; the existing Unity repository and previous builds remain preserved.

Hostinger instructions: https://www.hostinger.com/support/1583249-how-to-manage-dns-records-at-hostinger/

