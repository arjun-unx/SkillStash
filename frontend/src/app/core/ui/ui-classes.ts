/** Reusable Tailwind class bundles — no SCSS, DRY utilities only. */
export const UI = {
  page: 'mx-auto w-full max-w-6xl px-4 py-8 sm:px-6 lg:px-8',
  pageHeader: 'mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between',
  h1: 'text-2xl font-semibold tracking-tight text-white sm:text-3xl',
  h2: 'text-xl font-semibold tracking-tight text-white',
  muted: 'text-sm text-slate-400',
  dim: 'text-xs text-slate-500',
  label: 'mb-1.5 block text-sm font-medium text-slate-300',
  hint: 'mt-1 text-xs text-slate-500',
  error: 'mt-1 text-xs text-rose-400',
  input:
    'block w-full rounded-xl border border-line bg-surface-raised/80 px-3.5 py-2.5 text-sm text-slate-100 shadow-inner shadow-black/20 placeholder:text-slate-500 transition focus:border-violet-500/70 focus:outline-none focus:ring-2 focus:ring-violet-500/25 disabled:cursor-not-allowed disabled:opacity-50',
  select:
    'block w-full rounded-xl border border-line bg-surface-raised/80 px-3.5 py-2.5 text-sm text-slate-100 shadow-inner shadow-black/20 transition focus:border-violet-500/70 focus:outline-none focus:ring-2 focus:ring-violet-500/25',
  textarea:
    'block w-full resize-y rounded-xl border border-line bg-surface-raised/80 px-3.5 py-3 font-mono text-sm leading-relaxed text-slate-100 shadow-inner shadow-black/20 placeholder:text-slate-500 transition focus:border-violet-500/70 focus:outline-none focus:ring-2 focus:ring-violet-500/25',
  btnPrimary:
    'inline-flex items-center justify-center gap-2 rounded-xl bg-violet-600 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-violet-600/30 transition hover:bg-violet-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-400 disabled:cursor-not-allowed disabled:bg-violet-600/35 disabled:text-white/55 disabled:shadow-none',
  btnSecondary:
    'inline-flex items-center justify-center gap-2 rounded-xl border border-line-strong bg-surface-raised/60 px-4 py-2.5 text-sm font-medium text-slate-200 transition hover:border-slate-500 hover:bg-surface-overlay hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-400 disabled:cursor-not-allowed disabled:opacity-45',
  btnGhost:
    'inline-flex items-center justify-center gap-2 rounded-xl px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-white/5 hover:text-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-500',
  btnDanger:
    'inline-flex items-center justify-center gap-2 rounded-xl border border-rose-500/40 bg-rose-500/10 px-4 py-2.5 text-sm font-medium text-rose-300 transition hover:border-rose-400/60 hover:bg-rose-500/20 hover:text-rose-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-rose-400',
  btnIcon:
    'inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-slate-400 transition hover:bg-white/5 hover:text-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-400',
  card: 'rounded-2xl border border-line bg-surface-elevated/80 p-5 shadow-card backdrop-blur-sm transition hover:border-line-strong',
  badge:
    'inline-flex items-center rounded-full bg-violet-500/15 px-2.5 py-0.5 text-[11px] font-semibold uppercase tracking-wide text-accent-muted',
  avatar:
    'inline-flex shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-violet-600 to-cyan-500 font-semibold text-white',
  avatarSm: 'h-6 w-6 text-[11px]',
  avatarMd: 'h-8 w-8 text-sm',
  avatarXl: 'h-14 w-14 text-xl',
  icon: 'material-symbols-outlined text-[20px] leading-none',
  gridCards: 'grid gap-5 sm:grid-cols-2 xl:grid-cols-2',
  navLink:
    'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-400 transition hover:bg-white/5 hover:text-white',
  navLinkActive: 'bg-violet-500/15 text-white ring-1 ring-inset ring-violet-500/25'
} as const;
