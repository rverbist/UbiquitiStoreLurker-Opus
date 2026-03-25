import { createRouter, createWebHistory } from 'vue-router';
import MonitorView from '@/views/MonitorView.vue';
import SetupView from '@/views/SetupView.vue';

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: MonitorView },
    { path: '/setup', component: SetupView },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
});
