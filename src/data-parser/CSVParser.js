/**
 * CSVParser - CSV 파일 파싱
 * 주석 처리, 큰따옴표 이스케이프, 타입 자동 변환 지원
 */
import { gameLogger } from '../core/Logger.js';

class CSVParser {
    /**
     * CSV 텍스트 파싱
     * @param {string} text - CSV 텍스트
     * @returns {Object[]} 객체 배열
     */
    static parse(text) {
        const lines = text.split(/\r?\n/);
        const result = [];
        
        // 헤더 행 추출
        let headerIndex = 0;
        while (headerIndex < lines.length) {
            const line = lines[headerIndex].trim();
            // 주석이나 빈 행 스킵
            if (line && !line.startsWith('#')) {
                break;
            }
            headerIndex++;
        }
        
        if (headerIndex >= lines.length) {
            gameLogger.warn('CSV has no data');
            return [];
        }
        
        const headers = this.parseLine(lines[headerIndex]);
        
        // 데이터 행 파싱
        for (let i = headerIndex + 1; i < lines.length; i++) {
            const line = lines[i].trim();
            
            // 주석, 빈 행 스킵
            if (!line || line.startsWith('#')) continue;
            
            const values = this.parseLine(line);
            const row = {};
            
            headers.forEach((header, index) => {
                const value = values[index];
                row[header] = this.convertType(value);
            });
            
            result.push(row);
        }
        
        return result;
    }

    /**
     * CSV 행 파싱 (큰따옴표 처리 포함)
     * @param {string} line 
     * @returns {string[]}
     */
    static parseLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;
        
        for (let i = 0; i < line.length; i++) {
            const char = line[i];
            
            if (char === '"') {
                // 큰따옴표 이스케이프 확인
                if (inQuotes && line[i + 1] === '"') {
                    current += '"';
                    i++; // 다음 문자 스킵
                } else {
                    inQuotes = !inQuotes;
                }
            } else if (char === ',' && !inQuotes) {
                result.push(current.trim());
                current = '';
            } else {
                current += char;
            }
        }
        
        // 마지막 필드 추가
        result.push(current.trim());
        
        return result;
    }

    /**
     * 타입 자동 변환
     * @param {string} value 
     * @returns {number|boolean|string|Object}
     */
    static convertType(value) {
        // 빈 문자열
        if (value === '') return null;
        
        // 불리언
        if (value.toLowerCase() === 'true') return true;
        if (value.toLowerCase() === 'false') return false;
        
        // 숫자
        if (/^-?\d+(\.\d+)?$/.test(value)) {
            return parseFloat(value);
        }
        
        // JSON 문자열 (중괄호로 시작)
        if (value.startsWith('{') && value.endsWith('}')) {
            try {
                // 큰따옴표 복원 (CSV 에서 이스케이프된 것)
                const jsonStr = value.replace(/""/g, '"');
                return JSON.parse(jsonStr);
            } catch (e) {
                gameLogger.warn('Failed to parse JSON value:', value);
                return value;
            }
        }
        
        // 문자열
        return value;
    }

    /**
     * CSV 파일에서 로드 (fetch)
     * @param {string} url - CSV 파일 URL
     * @returns {Promise<Object[]>}
     */
    static async parseFile(url) {
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`Failed to fetch ${url}: ${response.status}`);
            }
            const text = await response.text();
            return this.parse(text);
        } catch (error) {
            gameLogger.error(`Failed to load CSV ${url}:`, error);
            throw error;
        }
    }
}

export { CSVParser };
