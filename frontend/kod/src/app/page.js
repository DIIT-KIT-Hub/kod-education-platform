"use client";

import { useEffect, useState } from "react";

export default function Home() {
  const [data, setData] = useState(""); // стан для відповіді

  useEffect(() => {
    fetch("https://localhost:7122/api/Init")
      .then((res) => res.text()) // читаємо як текст
      .then((text) => {
        setData(text); // зберігаємо рядок у стані
      })
      .catch((err) => {
        console.error("Помилка запиту:", err);
        setData("Помилка при запиті до сервера");
      });
  }, []);

  return <div>{data}</div>; // просто виводимо рядок
}
