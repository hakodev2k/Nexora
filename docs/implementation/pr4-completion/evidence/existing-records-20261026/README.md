# Curated actual existing-record evidence

Synthetic-only, local Chromium UI/API/isolated SQL runs by shopping_completion. No credentials, browser storage or real personal data included. Exact raw artifact sources, SHA256 and byte counts are in [manifest.csv](manifest.csv). Ten PNGs were actually opened and visually inspected before curation; three JSON proofs and three JUnit reports accompany them (16 evidence files,688707bytes before this README/manifest).

| Viewport | Final cases | Captured proof |
|---|---|---|
|1440×900|2PASS,0FAIL/0SKIP,16.2s| Desktop Order form top/actions; Certification immutable renewal history |
|768×1024|2PASS,0FAIL/0SKIP,14.2s| Tablet Warranty form top/actions, fixed readable detail; Certification history |
|390×844|2PASS,0FAIL/0SKIP,13.1s| Mobile Warranty form top/actions; Certification history |

`*-dialog-top.png` and `*-dialog-actions.png` are actual viewport screenshots (`fullPage:false`) taken at two positions of the same scrollable form. Save and Cancel were scrolled into view; actual Shift+Tab→Tab focused Cancel and its rendered outline/shadow was asserted. Wheel outside the modal kept the document position unchanged on real scrollable backgrounds: desktop0/max897→0, tablet1311/max1311→1311, mobile1981/max1981→1981. This is post-fix shared modal scroll-lock evidence, not a claimed pre-fix runtime failure. History/detail images are full-page screenshots, not visible-viewport proof.

The projects exercise Chromium at these viewport sizes; they do not prove a physical mobile device, native touch gestures, Safari/Firefox, all WCAG or complete feature acceptance. Source is the uncommitted continuation of `a0215f7b3e85d33503cff9077b41cc065240cb81`; coordinator must reconcile final delivered commit/tree and CI. Relevant identities: Shopping screen `7947895EE432245093411AD9AC14ABEE3B02098580C1E4443B1581B587A03072`, Certification screen `A3F321AC0AC895DD6FC86C587E6D37E8AEF30281E9500C811098E4C03D4710DA`, functional spec `3A3990BFB21D697FD47B2AC537EB6B1964A37EC5C3B15719A005E1C4FF58EC55`, observed shared modal source `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`.

See [author/final scope report](../../workers/existing-shopping-certification-20261025.md). Earlier failed/oracle-diagnostic runs remain local in `web/Nexora.Web/artifacts/existing-records-review/`; their results were preserved in that report, not converted into PASS. Human product/UI acceptance remains pending.
