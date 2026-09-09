#!/usr/bin/env python3
"""Validate completion claims against a structured evidence ledger.

Input ledger JSON example:
See repository-root .ai/verification.md for the revision-bound ledger contract.
Exit 0 supported, 4 unsupported, 2 invalid.
"""
from __future__ import annotations
import argparse, json, sys
from pathlib import Path


def load(path: Path) -> dict:
    try:
        obj=json.loads(path.read_text(encoding="utf-8"))
    except (OSError,json.JSONDecodeError) as exc:
        raise ValueError(f"cannot read {path}: {exc}") from exc
    if not isinstance(obj,dict): raise ValueError(f"{path} must contain object")
    return obj


def main() -> int:
    p=argparse.ArgumentParser(); p.add_argument("ledger",type=Path); p.add_argument("--policy",type=Path,required=True); a=p.parse_args()
    try:
        ledger,policy=load(a.ledger),load(a.policy)
        claims=ledger.get("claims",[]); evidence=ledger.get("evidence",[])
        if not isinstance(claims,list) or not claims or not all(isinstance(x,str) and x for x in claims): raise ValueError("claims must be nonempty string array")
        if not isinstance(evidence,list) or not all(isinstance(x,dict) for x in evidence): raise ValueError("evidence must be object array")
        revision=ledger.get("revision")
        if not isinstance(revision,str) or not revision.strip(): raise ValueError("revision is required")
        present={e.get("type") for e in evidence if isinstance(e.get("type"),str) and e.get("fresh") is True and e.get("passed") is True and e.get("revision")==revision and isinstance(e.get("reference"),str) and e["reference"].strip()}
        reqs=policy.get("claim_requirements",{})
        if not isinstance(reqs,dict): raise ValueError("claim_requirements must be object")
        unsupported={}
        for claim in claims:
            required=reqs.get(claim)
            if not isinstance(required,list) or not required or not all(isinstance(x,str) and x for x in required):
                unsupported[claim]=["claim type has no policy"]
                continue
            missing=[r for r in required if r not in present]
            conflicting=[e.get("type") for e in evidence if e.get("type") in required and e.get("revision")==revision and e.get("fresh") is True and e.get("passed") is False]
            if conflicting: missing.append("contradictory failed evidence: " + ", ".join(sorted(set(conflicting))))
            if missing: unsupported[claim]=missing
        status="supported" if not unsupported else "blocked"
        print(json.dumps({"status":status,"claims":claims,"available_evidence":sorted(x for x in present if x),"unsupported":unsupported},indent=2))
        return 0 if not unsupported else 4
    except (ValueError,TypeError) as exc:
        print(json.dumps({"status":"invalid","error":str(exc)}),file=sys.stderr); return 2

if __name__=="__main__": raise SystemExit(main())
