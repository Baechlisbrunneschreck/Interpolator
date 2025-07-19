<script setup lang="ts">
import {
  Chart as ChartJs,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { computed } from "vue";
import { Line } from "vue-chartjs";

ChartJs.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend);

const datapoints = [40, 39, 10, 40, 39, 80, 40];

const data = computed(() => ({
  labels: datapoints.map((_, index) => index),
  datasets: [
    {
      label: "Data One",
      backgroundColor: "#f87979",
      data: datapoints,
    },
  ],
}));

const options = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
}));
</script>

<template>
  <header>
    <h1>Interpolator</h1>
    <p>Host Application</p>
  </header>
  <main>
    <Line :data="data" :options="options" />
  </main>
</template>
