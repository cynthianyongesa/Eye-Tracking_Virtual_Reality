#!/usr/bin/env python
# coding: utf-8

# In[1]:


# ============================================================
#  ROI_inspector.py
#  Get every unique LookedObject value across ALL participant files
# ============================================================

import os, glob
import pandas as pd
from collections import Counter

CSV_DIR  = "/Users/cynthianyongesa/Desktop/Desktop - Cynthia's Macbook Pro/DATA/2_EYE_TRACKING_PY/Eye-Tracking Excel Files"
LOOKED   = "LookedObject"

all_csvs = sorted(glob.glob(os.path.join(CSV_DIR, "*.csv")))
print(f"Scanning {len(all_csvs)} files...\n")

global_counter  = Counter()   # total frames per ROI name across all participants
per_file_counts = {}          # which files contain which ROIs

for path in all_csvs:
    pid = os.path.basename(path).split("_")[0]
    try:
        df  = pd.read_csv(path, sep="\t")
        if df.shape[1] < 5:
            df = pd.read_csv(path, sep=",")

        if LOOKED not in df.columns:
            print(f"  ⚠  {pid}: LookedObject column not found")
            continue

        counts = df[LOOKED].fillna("__NULL__").value_counts()
        for roi, n in counts.items():
            global_counter[roi] += n
            if roi not in per_file_counts:
                per_file_counts[roi] = []
            per_file_counts[roi].append(pid)

    except Exception as e:
        print(f"  ✗  {pid}: {e}")

# ── Print full unique ROI list sorted by total frame count ────
print(f"\n{'='*65}")
print(f"  UNIQUE LookedObject VALUES — across all {len(all_csvs)} files")
print(f"{'='*65}")
print(f"  {'ROI NAME':<40} {'TOTAL FRAMES':>12}  {'# FILES':>8}")
print(f"  {'-'*62}")

for roi, total_frames in global_counter.most_common():
    n_files = len(per_file_counts[roi])
    flag = "  ← NULL/empty" if roi == "__NULL__" else ""
    print(f"  {roi:<40} {total_frames:>12,}  {n_files:>8}{flag}")

print(f"\n  Total unique values: {len(global_counter)}")
print(f"{'='*65}\n")

