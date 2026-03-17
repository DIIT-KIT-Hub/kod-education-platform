import { useState } from "react";

export const useForm = (initialValues, validate) => {
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
    return (e) => {
      e.preventDefault();
      setErrors({});

      const validationErrors = validate(formData);

      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
        return;
      }

      callback(formData);
    };
  };

  return {
    formData,
    errors,
    setErrors,
    handleChange,
    handleSubmit,
  };
};
