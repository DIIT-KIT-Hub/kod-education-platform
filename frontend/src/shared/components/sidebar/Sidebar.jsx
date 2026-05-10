// CSR
"use client";

// Imports
import React, { useState } from "react";
import styles from "./Sidebar.module.css";
import { Link } from "@/i18n/routing";

/**
 * Sidebar navigation component for the application layout.
 *
 * Responsibilities:
 * - Renders navigation links based on provided routes
 * - Manages open/close state of sidebar
 * - Supports internationalized routing
 *
 * This component is purely presentational and relies on
 * pre-filtered routes (permission-based filtering happens server-side).
 *
 * @component
 *
 * @param {Object} props - Component props
 * @param {Array<Object>} props.routes - List of navigation routes
 * @param {string} props.routes[].path - Route path
 * @param {string} props.routes[].label - Display label for route
 *
 * @returns {JSX.Element} Rendered sidebar navigation
 */
function Sidebar({ routes }) {
  const [isOpen, setIsOpen] = useState(true);

  const toggleSidebar = () => setIsOpen(!isOpen);

  return (
    <nav className={`${styles.sidebar} ${!isOpen ? styles.closed : ""}`}>
      <div className={styles.header}>
        <button className={styles.toggle} onClick={toggleSidebar}>
          <svg
            width="30"
            height="30"
            viewBox="0 0 512 512"
            fill="white"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path d="M160 115.4L180.7 96L352 256L180.7 416L160 396.6L310.5 256L160 115.4Z" />
          </svg>
        </button>
      </div>

      <div className={styles.links}>
        {routes.map((route, index) => (
          <Link key={index} href={route.path}>
            {route.label}
          </Link>
        ))}
      </div>
    </nav>
  );
}

// Sidebar export
export default Sidebar;
