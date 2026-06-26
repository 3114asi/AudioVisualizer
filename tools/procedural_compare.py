import json
import subprocess
from pathlib import Path

import cv2
import numpy as np
from skimage.metrics import structural_similarity as ssim


ROOT = Path(__file__).resolve().parents[1]
REF_VIDEO = ROOT / "ref" / "1.mp4"
CURRENT = ROOT / "ref" / "automated_smoke.png"
KEYFRAMES = ROOT / "ref" / "keyframes_proc"
REPORT = ROOT / "Logs" / "ProceduralCompareReport.json"


def read_image(path):
    data = np.fromfile(str(path), dtype=np.uint8)
    img = cv2.imdecode(data, cv2.IMREAD_COLOR)
    if img is None:
        raise FileNotFoundError(path)
    return img


def extract_keyframes():
    KEYFRAMES.mkdir(parents=True, exist_ok=True)
    duration = float(subprocess.check_output(
        ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", str(REF_VIDEO)],
        cwd=ROOT,
        text=True,
    ).strip())
    frames = []
    for i in range(11):
        t = min(duration * i / 10.0, duration - 0.05)
        out = KEYFRAMES / f"frame_{i:02d}.png"
        subprocess.run(["ffmpeg", "-y", "-ss", f"{t:.4f}", "-i", str(REF_VIDEO), "-frames:v", "1", str(out)],
                       cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
        frames.append(out)
    return frames


def fit(img, size=(576, 880)):
    return cv2.resize(img, size, interpolation=cv2.INTER_AREA)


def masks(img):
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    bright = gray > 45
    bloom = gray > 120
    hot = gray > 205
    magenta = ((hsv[..., 0] > 135) | (hsv[..., 0] < 8)) & (hsv[..., 1] > 80) & (hsv[..., 2] > 60)
    cyan = (hsv[..., 0] > 75) & (hsv[..., 0] < 112) & (hsv[..., 1] > 60) & (hsv[..., 2] > 45)
    dark = gray < 8
    return hsv, gray, bright, bloom, hot, magenta, cyan, dark


def contour_metrics(mask, shape):
    contours, _ = cv2.findContours(mask.astype(np.uint8) * 255, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours:
        return {"area_ratio": 0, "cx": 0, "cy": 0, "radius": 0, "circularity": 0}
    cnt = max(contours, key=cv2.contourArea)
    area = cv2.contourArea(cnt)
    perimeter = max(cv2.arcLength(cnt, True), 1)
    (x, y), radius = cv2.minEnclosingCircle(cnt)
    h, w = shape[:2]
    return {
        "area_ratio": float(area / (w * h)),
        "cx": float(x / w),
        "cy": float(y / h),
        "radius": float(radius / min(w, h)),
        "circularity": float(4 * np.pi * area / (perimeter * perimeter)),
    }


def image_metrics(img):
    img = fit(img)
    hsv, gray, bright, bloom, hot, magenta, cyan, dark = masks(img)
    edges = cv2.Canny(gray, 40, 120)
    # Bright connected components approximate stars/particles.
    detail = cv2.threshold(cv2.subtract(cv2.GaussianBlur(gray, (0, 0), 1.0),
                                        cv2.GaussianBlur(gray, (0, 0), 7.0)), 16, 255, cv2.THRESH_BINARY)[1]
    n, _, stats, _ = cv2.connectedComponentsWithStats(detail)
    small = stats[1:, cv2.CC_STAT_AREA] if n > 1 else np.array([])
    particles = small[(small >= 1) & (small <= 45)]
    portal = contour_metrics(dark & (np.indices(gray.shape)[0] < gray.shape[0] * 0.75), img.shape)
    glow = contour_metrics(bright, img.shape)
    lower = gray[int(gray.shape[0] * 0.68):, :]
    upper = gray[:int(gray.shape[0] * 0.68), :]
    return {
        "mean_v": float(hsv[..., 2].mean()),
        "std_v": float(hsv[..., 2].std()),
        "mean_s": float(hsv[..., 1].mean()),
        "bright_ratio": float(bright.mean()),
        "bloom_ratio": float(bloom.mean()),
        "hot_ratio": float(hot.mean()),
        "magenta_ratio": float(magenta.mean()),
        "cyan_ratio": float(cyan.mean()),
        "edge_ratio": float((edges > 0).mean()),
        "particle_count": int(len(particles)),
        "portal_cx": portal["cx"],
        "portal_cy": portal["cy"],
        "portal_radius": portal["radius"],
        "portal_circularity": portal["circularity"],
        "glow_area": glow["area_ratio"],
        "lower_brightness": float(lower.mean()),
        "upper_brightness": float(upper.mean()),
        "water_reflection_ratio": float((lower > 80).mean()),
    }


def compare(ref, cur):
    ref = fit(ref)
    cur = fit(cur)
    rg = cv2.cvtColor(ref, cv2.COLOR_BGR2GRAY)
    cg = cv2.cvtColor(cur, cv2.COLOR_BGR2GRAY)
    hist = []
    for ch in range(3):
        h1 = cv2.calcHist([ref], [ch], None, [96], [0, 256])
        h2 = cv2.calcHist([cur], [ch], None, [96], [0, 256])
        cv2.normalize(h1, h1)
        cv2.normalize(h2, h2)
        hist.append(cv2.compareHist(h1, h2, cv2.HISTCMP_CORREL))
    e1 = cv2.Canny(rg, 40, 120)
    e2 = cv2.Canny(cg, 40, 120)
    edge = 1 - np.mean(np.abs(e1.astype(np.float32) - e2.astype(np.float32))) / 255
    rm = image_metrics(ref)
    cm = image_metrics(cur)
    errors = {}
    for k, v in rm.items():
        c = cm[k]
        errors[k] = float(abs(v - c) / max(abs(v), abs(c), 1.0))
    metric_similarity = 1 - float(np.mean(list(errors.values())))
    score = float(np.clip(0.40 * ssim(rg, cg) + 0.25 * np.mean(hist) + 0.18 * edge + 0.17 * metric_similarity, 0, 1))
    return score, {"hist": float(np.mean(hist)), "edge": float(edge), "metrics": metric_similarity, "errors": errors, "ref": rm, "cur": cm}


def main():
    frames = extract_keyframes()
    cur = read_image(CURRENT)
    results = []
    for frame in frames:
        score, data = compare(read_image(frame), cur)
        results.append({"frame": frame.name, "score": score, **data})
    best = max(results, key=lambda x: x["score"])
    avg = float(np.mean([r["score"] for r in results]))
    report = {
        "best_similarity_percent": best["score"] * 100,
        "average_similarity_percent": avg * 100,
        "best_frame": best["frame"],
        "best": best,
    }
    REPORT.parent.mkdir(exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({
        "best_similarity_percent": report["best_similarity_percent"],
        "average_similarity_percent": report["average_similarity_percent"],
        "best_frame": report["best_frame"],
    }, indent=2))


if __name__ == "__main__":
    main()
