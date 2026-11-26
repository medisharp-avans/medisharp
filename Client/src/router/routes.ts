//TODO
//  Add following meta information for routes:
// - requiresAuth: for routes where authentication is required
// - requiresRol(roltype): for routes that are only accessible for specific user roles
// - hideLayout: for routes that do not or should not inquire the base layout of the application, see App.vue

const routes = [
  //Auth routes
  // { path: '/login', name: 'Login', component: () => import("@/Login.vue"), meta: { hideLayout: true }},
  {
    path: '/profiel',
    name: 'Profile',
    component: () => import('@/views/profile/ProfileView.vue'),
    meta: { title: 'Profiel', requiresAuth: true },
  },

  // Main routes
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/HomeView.vue'),
    meta: { title: 'Home', requiresAuth: true },
  },
  {
    path: '/afspraken',
    name: 'Appointments',
    component: () => import('@/views/appointments/AppointmentsView.vue'),
    meta: { title: 'Afspraken', requiresAuth: true },
  },
  {
    path: '/afspraken/nieuw',
    name: 'NewAppointment',
    component: () => import('@/views/appointments/AppointmentsForm.vue'),
    meta: { title: 'Afspraak aanmaken', requiresAuth: true },
  },
  {
    path: '/medisch-dossier',
    name: 'MedicalFile',
    component: () => import('@/views/medicalFiles/MedicalFileView.vue'),
    meta: { title: 'Medisch dossier', requiresAuth: true },
  },
  {
    path: '/verwijsbrieven',
    name: 'Referrals',
    component: () => import('@/views/referrals/ReferralsView.vue'),
    meta: { title: 'Verwijsbrieven', requiresAuth: true },
  },
  {
    path: '/patienten',
    name: 'Patients',
    component: () => import('@/views/patients/PatientsView.vue'),
    meta: { title: 'Patiënten', requiresAuth: true },
  },
  {
    path: '/onderzoeksresultaten',
    name: 'MedicalResults',
    component: () => import('@/views/medicalResults/MedicalResultsView.vue'),
    meta: { title: 'Onderzoeksuitslagen', requiresAuth: true },
  },
  {
    path: '/declaraties',
    name: 'Claims',
    component: () => import('@/views/claims/ClaimsView.vue'),
    meta: { title: 'Declaraties', requiresAuth: true },
  },

  // Error routes
  {
    path: '/400',
    name: '400',
    component: () => import('@/views/errors/400View.vue'),
    meta: { title: 'Ongeldig verzoek', hideLayout: true },
  },
  {
    path: '/401',
    name: '401',
    component: () => import('@/views/errors/401View.vue'),
    meta: { title: 'Niet ingelogd', hideLayout: true },
  },
  {
    path: '/403',
    name: '403',
    component: () => import('@/views/errors/403View.vue'),
    meta: { title: 'Geen toegang', hideLayout: true },
  },
  {
    path: '/404',
    name: '404',
    component: () => import('@/views/errors/404View.vue'),
    meta: { title: 'Pagina niet gevonden', hideLayout: true },
  },
  {
    path: '/500',
    name: '500',
    component: () => import('@/views/errors/500View.vue'),
    meta: { title: 'Er is iets misgegaan', hideLayout: true },
  },

  // Catch all route
  { path: '/:pathMatch(.*)*', redirect: '/404' },
]

export default routes
