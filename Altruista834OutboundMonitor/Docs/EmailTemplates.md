# Email Templates

## 1) Missing vendor file alert

**Subject:** `[ALERT] Vendor extract file missing`

**Body:**
```
Process Date: {yyyy-MM-dd} IST
Step: VendorExtractUtility
Issue: Expected files (C, Pend_C) were not received within 06:00-08:30 IST.
Action: Please verify upstream vendor drop and Tidal schedule.
```

## 2) SLA breach notification (client)

**Subject:** `[NOTICE] Processing delay`

**Body:**
```
File: {fileName}
Estimated Completion: {estimateTime} IST
SLA Cutoff: 10:00 IST
Impact: SLA breach likely. Processing continues and updates will follow.
```
