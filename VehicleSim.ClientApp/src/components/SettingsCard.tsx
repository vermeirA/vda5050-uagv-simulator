import { FaCheck } from "react-icons/fa";
import { FaArrowRotateLeft } from "react-icons/fa6";
import axios from "axios";
import { useState } from "react";

type Method = "POST" | "PUT";

interface SettingsCardProps {
  title: string;
  description: string;
  value?: number;
  icon?: string;
  actionUrl: string;
  actionType?: Method;
  storageKey?: string;
}

const performAction = async (
  url: string,
  method: Method,
  value?: number
): Promise<boolean> => {
  try {
    if (method === "PUT") {
      if (!value || isNaN(value) || value <= 0) {
        console.error("Invalid value for PUT request");
        return false;
      }
      const response = await axios.put(`${url}/${value}`);
      return response.status >= 200 && response.status < 300;
    }

    const response = await axios.post(url);
    return response.status >= 200 && response.status < 300;
  } catch (error) {
    console.error(`Error performing ${method} request:`, error);
    return false;
  }
};

export const SettingsCard = ({
  title,
  description,
  value: initialValue,
  icon = "check",
  actionUrl,
  actionType = "POST",
  storageKey,
}: SettingsCardProps) => {
  const [inputValue, setInputValue] = useState<number | undefined>(() => {
    const stored = storageKey ? localStorage.getItem(storageKey) : null;
    const parsed = stored ? parseFloat(stored) : initialValue;
    return parsed && parsed >= 0.1 && parsed <= 100 ? parsed : initialValue;
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const parsed = parseFloat(e.target.value);
    setInputValue(
      !isNaN(parsed) ? Math.min(100, Math.max(0.1, parsed)) : undefined
    );
  };

  const handleClick = async () => {
    if (actionType === "PUT") {
      if (inputValue === undefined || inputValue < 0.1 || inputValue > 100) {
        console.error("Value must be between 0.1 and 100");
        return;
      }
    }

    const success = await performAction(actionUrl, actionType, inputValue);
    if (success && storageKey && inputValue !== undefined) {
      localStorage.setItem(storageKey, inputValue.toString());
    }
  };

  return (
    <div className="bg-[#1B2724] rounded-xl p-8 shadow-md hover:shadow-lg transition-shadow">
      <div className="flex justify-between items-center">
        <div className="flex-1">
          <h2 className="text-white text-2xl font-medium mb-2">{title}</h2>
          <p className="text-gray-300 text-md">{description}</p>
        </div>
        <div className="flex items-center gap-4 ml-8">
          {initialValue !== undefined && (
            <div className="relative">
              <input
                type="number"
                step="0.1"
                min="0.1"
                max="100"
                value={inputValue}
                onChange={handleInputChange}
                onBlur={(e) => {
                  const value = parseFloat(e.target.value);
                  if (isNaN(value) || value < 0.1) {
                    setInputValue(0.1);
                  } else if (value > 100) {
                    setInputValue(100);
                  }
                }}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleClick();
                  }
                }}
                className="bg-[#0f1a17] text-white text-xl font-medium w-18 text-center 
                  border-2 border-[#2a3f39] rounded-lg px-3 py-3 
                  focus:outline-none focus:border-[#00EA5E] focus:ring-2 focus:ring-[#00EA5E]/20
                  transition-all duration-200
                  [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
              <span className="absolute -bottom-5 left-1/2 -translate-x-1/2 text-xs text-gray-400">
                multiplier
              </span>
            </div>
          )}
          <button
            onClick={handleClick}
            disabled={!actionUrl}
            className="flex items-center justify-center w-14 h-14 
              bg-[#00EA5E]/10 hover:bg-[#00EA5E]/20 
              border-2 border-[#00EA5E]/30 hover:border-[#00EA5E]/50
              rounded-xl transition-all duration-200
              disabled:opacity-50 disabled:cursor-not-allowed 
              active:scale-95"
            aria-label={icon === "check" ? "Confirm" : "Reset"}
          >
            {icon === "check" ? (
              <FaCheck className="text-[#00EA5E]" size={24} strokeWidth={2.5} />
            ) : (
              <FaArrowRotateLeft
                className="text-[#00EA5E]"
                size={26}
                strokeWidth={2.5}
              />
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default SettingsCard;
