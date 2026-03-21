import { useState } from "react";

export const useForm = (initialValues, validate) => {
  const [isLoading, setIsLoading] = useState(false);
  const [formData, setFormData] = useState(initialValues);
  const [errors, setErrors] = useState({});

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = (callback) => {
    return async (e) => {
      e.preventDefault();
      setErrors({});

      const validationErrors = validate(formData);

      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
        return;
      }

      try {
        setIsLoading(true);

        await callback(formData);
      } finally {
        setIsLoading(false);
      }
    };
  };

  return {
    isLoading,
    formData,
    errors,
    setErrors,
    handleChange,
    handleSubmit,
  };
};
