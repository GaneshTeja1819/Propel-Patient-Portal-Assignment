import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuthContext } from '../../context/AuthContext';
import styles from './AdminPortalLayout.module.css';

interface Props {
  children: ReactNode;
}

export function AdminPortalLayout({ children }: Props) {
  const { logout } = useAuthContext();

  return (
    <div className={styles.root}>
      <header className={styles.topnav} role="banner">
        <div className={styles.topnavInner}>
          {/* Left: logo + nav links */}
          <div className={styles.navLeft}>
            <div className={styles.navLogoMark} aria-hidden="true">
              A
            </div>
            <span className={styles.navBrand}>Admin Portal</span>
            <nav className={styles.navLinks} aria-label="Admin navigation">
              <NavLink
                to="/admin/users"
                className={({ isActive }) =>
                  isActive ? `${styles.navLink} ${styles.active}` : styles.navLink
                }
              >
                👥 Users
              </NavLink>
              <NavLink
                to="/admin/audit-log"
                className={({ isActive }) =>
                  isActive ? `${styles.navLink} ${styles.active}` : styles.navLink
                }
              >
                📋 Audit Log
              </NavLink>
            </nav>
          </div>

          {/* Right: role badge + sign out */}
          <div className={styles.navRight}>
            <span className={styles.roleBadge}>🔑 Admin</span>
            <button
              type="button"
              className={styles.btnSignout}
              onClick={() => void logout()}
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className={styles.content}>{children}</main>
    </div>
  );
}
