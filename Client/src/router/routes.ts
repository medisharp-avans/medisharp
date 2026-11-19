//TODO
//Add following meta information for routes:
// - requiresAuth: for routes where authentication is required
// - requiresRol(roltype): for routes that are only accessible for specific user roles
// - hideLayout: for routes that do not or should not inquire the base layout of the application, see App.vue

const routes = [
    //Auth routes
    // { path: '/login', name: 'Login', component: () => import("@/Login.vue"), meta: { hideLayout: true }},

    // Main routes
    { path: '/', name: 'Home', component: () => import('@/views/HomeView.vue') },

    // Error routes
    { path: '/400', name: '400', component: () => import('@/views/errors/400.vue'), meta: { hideLayout: true }},
    { path: '/401', name: '401', component: () => import('@/views/errors/401.vue'), meta: { hideLayout: true }},
    { path: '/403', name: '403', component: () => import('@/views/errors/403.vue'), meta: { hideLayout: true }},
    { path: '/404', name: '404', component: () => import('@/views/errors/404.vue'), meta: { hideLayout: true }},
    { path: '/500', name: '500', component: () => import('@/views/errors/500.vue'), meta: { hideLayout: true }},

    // Catch all route
    { path: '/:pathMatch(.*)*',  redirect: '/404' }
];

export default routes;