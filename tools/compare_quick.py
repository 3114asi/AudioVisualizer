"""Quick visual comparison: current.png vs reference frame(s) from 1.mp4"""
import sys, subprocess, json
from pathlib import Path
import cv2
import numpy as np

ROOT = Path(r"C:\Users\edisk\OneDrive\Документы\Программирование\Android\AudioVisualizer")
CURRENT = ROOT / "ref" / "current.png"
REF_IMAGE = ROOT / "ref" / "1.png"   # single reference frame
VIDEO = ROOT / "ref" / "1.mp4"
FRAMES_DIR = ROOT / "ref" / "keyframes"

FRAMES_DIR.mkdir(exist_ok=True)

W, H = 576, 880

def fit(img): return cv2.resize(img, (W, H), interpolation=cv2.INTER_AREA)
def read(p):
    data = np.fromfile(str(p), dtype=np.uint8)
    img = cv2.imdecode(data, cv2.IMREAD_COLOR)
    if img is None: raise FileNotFoundError(p)
    return img

def extract_frames(video, n=11):
    """Extract n evenly-spaced frames from video."""
    result = subprocess.run(
        ["ffprobe","-v","error","-show_entries","format=duration","-of","default=nw=1:nk=1",str(video)],
        capture_output=True, text=True)
    dur = float(result.stdout.strip()) if result.returncode == 0 else 10.0
    paths = []
    for i in range(n):
        t = min(dur * i / max(n-1,1), dur-0.05)
        out = FRAMES_DIR / f"frame_{i:02d}.png"
        subprocess.run(["ffmpeg","-y","-ss",f"{t:.3f}","-i",str(video),"-frames:v","1",str(out)],
                       capture_output=True)
        if out.exists(): paths.append(out)
    return paths

def hsv_metrics(img):
    h = cv2.cvtColor(img, cv2.COLOR_BGR2HSV).astype(float)
    g = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(float)
    return {
        "mean_v": h[...,2].mean(),
        "mean_s": h[...,1].mean(),
        "bright": (g>45).mean(),
        "bloom":  (g>120).mean(),
        "hot":    (g>205).mean(),
        "cyan":   ((h[...,0]>78)&(h[...,0]<115)&(h[...,1]>55)&(h[...,2]>40)).mean(),
        "magenta":((h[...,0]>135)|(h[...,0]<8)) if False else ((h[...,0]>135)|(h[...,0]<10)) and True,
    }

def ring_metrics(img):
    """Detect ring: dark circle center, bright ring edge."""
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    blur = cv2.GaussianBlur(gray, (0,0), 3)
    _, bw = cv2.threshold(blur, 40, 255, cv2.THRESH_BINARY)
    contours, _ = cv2.findContours(bw, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours: return {}
    # Pick most circular contour in upper 65% of image (ring should be there)
    def circularity(c):
        a = cv2.contourArea(c)
        p = cv2.arcLength(c, True)
        return 4*np.pi*a/(p*p+1e-9) if p > 0 else 0
    upper_contours = [c for c in contours if cv2.minEnclosingCircle(c)[0][1] < H*0.72]
    if upper_contours:
        best = max(upper_contours, key=circularity)
    else:
        best = max(contours, key=cv2.contourArea)
    area = cv2.contourArea(best)
    peri = cv2.arcLength(best, True)
    (cx,cy),r = cv2.minEnclosingCircle(best)
    circ = 4*np.pi*area/(peri*peri+1e-9)
    # Color at ring positions
    ring_mask = bw.copy()
    ring_mask[:] = 0
    cv2.circle(ring_mask, (int(cx),int(cy)), int(r)+3, 255, 18)
    ring_pix = img[ring_mask>0]
    left  = img[ring_mask>0, :]
    # left half: x < cx, right half: x > cx
    ys,xs = np.where(ring_mask>0)
    left_mask  = xs < cx
    right_mask = xs > cx
    left_color  = img[ys[left_mask], xs[left_mask]].mean(axis=0) if left_mask.any() else [0,0,0]
    right_color = img[ys[right_mask], xs[right_mask]].mean(axis=0) if right_mask.any() else [0,0,0]
    return {
        "ring_cx_norm": float(cx/W), "ring_cy_norm": float(cy/H),
        "ring_r_norm": float(r/min(W,H)),
        "ring_circularity": float(circ),
        "left_BGR": left_color.tolist(),   # BGR - left should be blue
        "right_BGR": right_color.tolist(), # BGR - right should be magenta
    }

def ssim_score(a, b):
    from skimage.metrics import structural_similarity
    ag = cv2.cvtColor(a, cv2.COLOR_BGR2GRAY)
    bg = cv2.cvtColor(b, cv2.COLOR_BGR2GRAY)
    s, _ = structural_similarity(ag, bg, full=True, data_range=255)
    return float(s)

def hist_match(a, b):
    """Per-channel histogram correlation."""
    scores = []
    for c in range(3):
        ha = cv2.calcHist([a],[c],None,[256],[0,256])
        hb = cv2.calcHist([b],[c],None,[256],[0,256])
        scores.append(cv2.compareHist(ha, hb, cv2.HISTCMP_CORREL))
    return float(np.mean(scores))

# ── Main ──────────────────────────────────────────────────────────────
cur = fit(read(CURRENT))

# Use static reference image
ref = fit(read(REF_IMAGE))

# Also try video frames if ffmpeg available
frame_paths = extract_frames(VIDEO, 5) if VIDEO.exists() else []
frame_imgs = [fit(read(p)) for p in frame_paths]

ssim_ref   = ssim_score(cur, ref)
hist_ref   = hist_match(cur, ref)

frame_scores = [(ssim_score(cur,f), hist_match(cur,f)) for f in frame_imgs]

ref_m = ring_metrics(ref)
cur_m = ring_metrics(cur)

print("=== Comparison Results ===")
print(f"SSIM vs ref image:     {ssim_ref:.4f}")
print(f"Histogram vs ref:      {hist_ref:.4f}")
if frame_scores:
    best_ssim = max(s[0] for s in frame_scores)
    best_hist = max(s[1] for s in frame_scores)
    print(f"Best SSIM vs video:    {best_ssim:.4f}")
    print(f"Best Hist vs video:    {best_hist:.4f}")

print(f"\n--- Ring Metrics ---")
print(f"Ref  ring center: ({ref_m.get('ring_cx_norm',0):.3f}, {ref_m.get('ring_cy_norm',0):.3f})  r={ref_m.get('ring_r_norm',0):.3f}  circ={ref_m.get('ring_circularity',0):.3f}")
print(f"Curr ring center: ({cur_m.get('ring_cx_norm',0):.3f}, {cur_m.get('ring_cy_norm',0):.3f})  r={cur_m.get('ring_r_norm',0):.3f}  circ={cur_m.get('ring_circularity',0):.3f}")
print(f"Ref  left(blue) BGR={[int(x) for x in ref_m.get('left_BGR',[0,0,0])]}  right(magenta) BGR={[int(x) for x in ref_m.get('right_BGR',[0,0,0])]}")
print(f"Curr left(blue) BGR={[int(x) for x in cur_m.get('left_BGR',[0,0,0])]}  right(magenta) BGR={[int(x) for x in cur_m.get('right_BGR',[0,0,0])]}")

overall = (ssim_ref*0.5 + hist_ref*0.3 + ref_m.get('ring_circularity',0)*0.2)
print(f"\nOverall similarity estimate: {overall*100:.1f}%")

# Save diff image
diff = cv2.absdiff(cur, ref)
diff_enhanced = cv2.convertScaleAbs(diff, alpha=3.0)
cv2.imwrite(str(ROOT/"ref"/"diff.png"), diff_enhanced)
print("Diff image saved → ref/diff.png")
