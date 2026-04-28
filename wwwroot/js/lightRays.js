import { Renderer, Program, Triangle, Mesh } from "https://esm.sh/ogl@1.0.11";

const hexToRgb = hex => {
  const m = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  return m ? [parseInt(m[1], 16) / 255, parseInt(m[2], 16) / 255, parseInt(m[3], 16) / 255] : [1, 1, 1];
};

const getAnchorAndDir = (origin, w, h) => {
  const outside = 0.2;
  switch (origin) {
    case 'top-left':      return { anchor: [0,             -outside * h], dir: [0,  1] };
    case 'top-right':     return { anchor: [w,             -outside * h], dir: [0,  1] };
    case 'left':          return { anchor: [-outside * w,    0.5 * h],    dir: [1,  0] };
    case 'right':         return { anchor: [(1+outside)*w,   0.5 * h],    dir: [-1, 0] };
    case 'bottom-left':   return { anchor: [0,          (1+outside)*h],   dir: [0, -1] };
    case 'bottom-center': return { anchor: [0.5*w,      (1+outside)*h],   dir: [0, -1] };
    case 'bottom-right':  return { anchor: [w,          (1+outside)*h],   dir: [0, -1] };
    default:              return { anchor: [0.5*w,          -outside*h],  dir: [0,  1] };
  }
};

const vert = `
attribute vec2 position;
varying vec2 vUv;
void main() {
  vUv = position * 0.5 + 0.5;
  gl_Position = vec4(position, 0.0, 1.0);
}`;

const frag = `precision highp float;

uniform float iTime;
uniform vec2  iResolution;
uniform vec2  rayPos;
uniform vec2  rayDir;
uniform vec3  raysColor;
uniform float raysSpeed;
uniform float lightSpread;
uniform float rayLength;
uniform float pulsating;
uniform float fadeDistance;
uniform float saturation;
uniform vec2  mousePos;
uniform float mouseInfluence;
uniform float noiseAmount;
uniform float distortion;

varying vec2 vUv;

float noise(vec2 st) {
  return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

float rayStrength(vec2 raySource, vec2 rayRefDirection, vec2 coord,
                  float seedA, float seedB, float speed) {
  vec2 sourceToCoord = coord - raySource;
  vec2 dirNorm = normalize(sourceToCoord);
  float cosAngle = dot(dirNorm, rayRefDirection);
  float distortedAngle = cosAngle + distortion * sin(iTime * 2.0 + length(sourceToCoord) * 0.01) * 0.2;
  float spreadFactor = pow(max(distortedAngle, 0.0), 1.0 / max(lightSpread, 0.001));
  float distance = length(sourceToCoord);
  float maxDistance = iResolution.x * rayLength;
  float lengthFalloff = clamp((maxDistance - distance) / maxDistance, 0.0, 1.0);
  float fadeFalloff = clamp(
    (iResolution.x * fadeDistance - distance) / (iResolution.x * fadeDistance),
    0.5, 1.0
  );
  float pulse = pulsating > 0.5 ? (0.8 + 0.2 * sin(iTime * speed * 3.0)) : 1.0;
  float baseStrength = clamp(
    (0.45 + 0.15 * sin(distortedAngle * seedA + iTime * speed)) +
    (0.3  + 0.2  * cos(-distortedAngle * seedB + iTime * speed)),
    0.0, 1.0
  );
  return baseStrength * lengthFalloff * fadeFalloff * spreadFactor * pulse;
}

void main() {
  vec2 coord = vec2(gl_FragCoord.x, iResolution.y - gl_FragCoord.y);

  vec2 finalRayDir = rayDir;
  if (mouseInfluence > 0.0) {
    vec2 mousePx  = mousePos * iResolution.xy;
    vec2 mouseDir = normalize(mousePx - rayPos);
    finalRayDir = normalize(mix(rayDir, mouseDir, mouseInfluence));
  }

  float r1 = rayStrength(rayPos, finalRayDir, coord, 36.2214, 21.11349, 1.5 * raysSpeed);
  float r2 = rayStrength(rayPos, finalRayDir, coord, 22.3991, 18.0234,  1.1 * raysSpeed);
  float strength = r1 * 0.5 + r2 * 0.4;

  vec3 col = vec3(strength);

  if (noiseAmount > 0.0) {
    float n = noise(coord * 0.01 + iTime * 0.1);
    col *= (1.0 - noiseAmount + noiseAmount * n);
  }

  /* Gradiente de cor vertical: azul-branco no topo, quase nulo no fundo */
  float brightness = 1.0 - (coord.y / iResolution.y);
  col.x *= 0.1 + brightness * 0.8;
  col.y *= 0.3 + brightness * 0.6;
  col.z *= 0.5 + brightness * 0.5;

  if (saturation != 1.0) {
    float gray = dot(col, vec3(0.299, 0.587, 0.114));
    col = mix(vec3(gray), col, saturation);
  }

  col *= raysColor;

  gl_FragColor = vec4(col, strength);
}`;

/**
 * Renderiza raios de luz em WebGL dentro de `container`.
 * Retorna uma função de limpeza.
 */
export function initLightRays(container, options) {
  const {
    raysOrigin     = 'top-center',
    raysColor      = '#ffffff',
    raysSpeed      = 1,
    lightSpread    = 1,
    rayLength      = 2,
    pulsating      = false,
    fadeDistance   = 1.0,
    saturation     = 1.0,
    followMouse    = true,
    mouseInfluence = 0.1,
    noiseAmount    = 0.0,
    distortion     = 0.0,
  } = options || {};

  const renderer = new Renderer({
    dpr: Math.min(window.devicePixelRatio, 2),
    alpha: true,
    premultipliedAlpha: false,
  });
  const gl = renderer.gl;

  /* Blending aditivo: os raios somam luz ao fundo */
  gl.enable(gl.BLEND);
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE);
  gl.clearColor(0, 0, 0, 0);

  const canvas = gl.canvas;
  canvas.style.cssText =
    'position:absolute;inset:0;width:100%;height:100%;display:block;pointer-events:none;';
  container.appendChild(canvas);

  const uniforms = {
    iTime:          { value: 0 },
    iResolution:    { value: [1, 1] },
    rayPos:         { value: [0, 0] },
    rayDir:         { value: [0, 1] },
    raysColor:      { value: hexToRgb(raysColor) },
    raysSpeed:      { value: raysSpeed },
    lightSpread:    { value: lightSpread },
    rayLength:      { value: rayLength },
    pulsating:      { value: pulsating ? 1.0 : 0.0 },
    fadeDistance:   { value: fadeDistance },
    saturation:     { value: saturation },
    mousePos:       { value: [0.5, 0.5] },
    mouseInfluence: { value: mouseInfluence },
    noiseAmount:    { value: noiseAmount },
    distortion:     { value: distortion },
  };

  const geometry = new Triangle(gl);
  const program  = new Program(gl, { vertex: vert, fragment: frag, uniforms });
  const mesh     = new Mesh(gl, { geometry, program });

  let rawMouse    = { x: 0.5, y: 0.5 };
  let smoothMouse = { x: 0.5, y: 0.5 };
  let animId;

  const updatePlacement = () => {
    renderer.dpr = Math.min(window.devicePixelRatio, 2);
    const { clientWidth: wCSS, clientHeight: hCSS } = container;
    renderer.setSize(wCSS, hCSS);
    const dpr = renderer.dpr;
    const w = wCSS * dpr, h = hCSS * dpr;
    uniforms.iResolution.value = [w, h];
    const { anchor, dir } = getAnchorAndDir(raysOrigin, w, h);
    uniforms.rayPos.value = anchor;
    uniforms.rayDir.value = dir;
  };

  const loop = t => {
    animId = requestAnimationFrame(loop);
    uniforms.iTime.value = t * 0.001;
    if (followMouse && mouseInfluence > 0) {
      const lf = 0.08;
      smoothMouse.x += (rawMouse.x - smoothMouse.x) * lf;
      smoothMouse.y += (rawMouse.y - smoothMouse.y) * lf;
      uniforms.mousePos.value = [smoothMouse.x, smoothMouse.y];
    }
    renderer.render({ scene: mesh });
  };

  const onMouseMove = e => {
    const rect = container.getBoundingClientRect();
    rawMouse.x = (e.clientX - rect.left) / rect.width;
    rawMouse.y = (e.clientY - rect.top)  / rect.height;
  };

  window.addEventListener('resize', updatePlacement);
  if (followMouse) window.addEventListener('mousemove', onMouseMove);
  updatePlacement();
  animId = requestAnimationFrame(loop);

  return function cleanup() {
    cancelAnimationFrame(animId);
    window.removeEventListener('resize', updatePlacement);
    if (followMouse) window.removeEventListener('mousemove', onMouseMove);
    gl.getExtension('WEBGL_lose_context')?.loseContext();
    if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
  };
}
