<script setup>
    import { RouterLink } from 'vue-router';
    import { useAuthStore } from '@/stores/Auth';
</script>

<template>
    <main class="error-container" aria-label="Foutmelding" tabindex="0" aria-live="assertive" aria-atomic="true">
        <div class="error-card">
            <h1 class="error-title">
                <slot name="title">Er is iets misgegaan.</slot>
            </h1>
        
            <div class="error-message">
                <slot>Er is een onverwachte fout opgetreden.</slot>
            </div>
            
            <div class="error-buttons">
                <RouterLink :to="{ name: 'home' }" class="btn-home">Terug naar Home</RouterLink>
                <RouterLink :to="{ name: 'login' }" @click="useAuthStore().logout()" class="btn-home">Terug naar Login</RouterLink>
            </div>
        </div>
    </main>
</template>

<style scoped>
    .error-container {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        width: 100%;
        background-color: #f9fafb; 
        padding: 1.5rem;
        box-sizing: border-box;
    }

    .error-card {
        text-align: center;
        max-width: 28rem;
        width: 100%;
        padding: 2.5rem 2rem;
        background-color: #ffffff;
        border-radius: 8px;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
        font-family: system-ui, -apple-system, sans-serif;
    }

    .error-title {
        font-size: 2rem;
        font-weight: 700;
        color: #1f2937; 
        margin: 0 0 1rem 0;
        line-height: 1.2;
    }

    .error-message {
        font-size: 1.125rem;
        color: #4b5563;
        margin-bottom: 2rem;
        line-height: 1.6;
    }

    .error-buttons {
        display: flex;
        flex-direction: row;
        gap: 1.5rem;
    }

    .btn-home {
        display: inline-block;
        padding: 0.75rem 1.5rem;
        background-color: #15803d; 
        color: #ffffff;
        font-weight: 600;
        font-size: 1rem;
        text-decoration: none;
        border-radius: 6px;
        transition: background-color 0.2s ease;
        &:hover {
            background-color: #166534;
        }
        &:focus-visible {
            outline: 3px solid #22c55e;
            outline-offset: 2px;
        }
    }
</style>
