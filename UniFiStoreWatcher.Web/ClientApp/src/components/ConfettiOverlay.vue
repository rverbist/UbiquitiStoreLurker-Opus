<script setup lang="ts">
import confetti from 'canvas-confetti';

defineExpose({ fire });

function fire() {
  confetti({
    particleCount: 120,
    spread: 70,
    origin: { y: 0.6 },
    colors: ['#006fff', '#338aff', '#ffd700', '#ffffff', '#2fb343'],
  });
  // Second burst for extra flair
  setTimeout(() => {
    confetti({
      particleCount: 60,
      spread: 100,
      origin: { y: 0.4 },
      angle: 60,
      colors: ['#006fff', '#ffd700'],
    });
    confetti({
      particleCount: 60,
      spread: 100,
      origin: { y: 0.4 },
      angle: 120,
      colors: ['#006fff', '#ffd700'],
    });
  }, 200);
}
</script>

<template>
  <!-- Canvas-confetti renders into its own canvas; this component is invisible -->
  <div />
</template>
