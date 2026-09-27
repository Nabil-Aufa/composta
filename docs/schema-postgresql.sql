--
-- PostgreSQL database dump
--

\restrict HRRoauPzSOHwBDdH71AGjq50pbxNzQea4uCVcqoOWIJ8rMifrsHQqrdwafUKTle

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: badges; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.badges (
    id character varying(36) NOT NULL,
    name character varying(50) NOT NULL,
    criteria character varying(50) NOT NULL,
    icon_path character varying(255)
);


--
-- Name: compost_batches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.compost_batches (
    id character varying(36) NOT NULL,
    user_id character varying(36) NOT NULL,
    start_date timestamp without time zone NOT NULL,
    status character varying(10) DEFAULT 'Active'::character varying NOT NULL,
    brown_green_ratio numeric(5,2) DEFAULT 0 NOT NULL,
    estimated_maturity_date timestamp without time zone NOT NULL,
    CONSTRAINT compost_batches_status_check CHECK (((status)::text = ANY ((ARRAY['Active'::character varying, 'Curing'::character varying, 'Ready'::character varying, 'Archived'::character varying])::text[])))
);


--
-- Name: compost_recommendations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.compost_recommendations (
    id character varying(36) NOT NULL,
    batch_id character varying(36) NOT NULL,
    message character varying(500) NOT NULL,
    priority character varying(10) DEFAULT 'Low'::character varying NOT NULL,
    created_at timestamp without time zone NOT NULL,
    CONSTRAINT compost_recommendations_priority_check CHECK (((priority)::text = ANY ((ARRAY['Low'::character varying, 'Medium'::character varying, 'High'::character varying])::text[])))
);


--
-- Name: maintenance_schedules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.maintenance_schedules (
    id character varying(36) NOT NULL,
    batch_id character varying(36) NOT NULL,
    task_type character varying(15) NOT NULL,
    due_date timestamp without time zone NOT NULL,
    is_completed boolean DEFAULT false NOT NULL,
    CONSTRAINT maintenance_schedules_task_type_check CHECK (((task_type)::text = ANY ((ARRAY['Turn'::character varying, 'Water'::character varying, 'CheckMoisture'::character varying, 'Harvest'::character varying])::text[])))
);


--
-- Name: user_badges; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_badges (
    user_id character varying(36) NOT NULL,
    badge_id character varying(36) NOT NULL,
    earned_at timestamp without time zone NOT NULL
);


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    id character varying(36) NOT NULL,
    name character varying(100) NOT NULL,
    email character varying(255) NOT NULL,
    joined_at timestamp without time zone NOT NULL
);


--
-- Name: waste_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.waste_categories (
    id character varying(36) NOT NULL,
    name character varying(50) NOT NULL,
    co2e_factor_per_kg numeric(6,3) DEFAULT 0 NOT NULL,
    is_compostable boolean DEFAULT true NOT NULL
);


--
-- Name: waste_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.waste_items (
    id character varying(36) NOT NULL,
    batch_id character varying(36) NOT NULL,
    category_id character varying(36),
    item_type character varying(15) NOT NULL,
    name character varying(100) NOT NULL,
    weight numeric(8,2) NOT NULL,
    date_added timestamp without time zone NOT NULL,
    brown_green_type character varying(5),
    moisture_content numeric(5,2),
    reason character varying(255),
    CONSTRAINT waste_items_brown_green_type_check CHECK (((brown_green_type)::text = ANY ((ARRAY['Brown'::character varying, 'Green'::character varying])::text[]))),
    CONSTRAINT waste_items_item_type_check CHECK (((item_type)::text = ANY ((ARRAY['compostable'::character varying, 'noncompostable'::character varying])::text[]))),
    CONSTRAINT waste_items_moisture_content_check CHECK (((moisture_content >= (0)::numeric) AND (moisture_content <= (100)::numeric))),
    CONSTRAINT waste_items_weight_check CHECK ((weight >= (0)::numeric))
);


--
-- Name: badges badges_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.badges
    ADD CONSTRAINT badges_pkey PRIMARY KEY (id);


--
-- Name: compost_batches compost_batches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.compost_batches
    ADD CONSTRAINT compost_batches_pkey PRIMARY KEY (id);


--
-- Name: compost_recommendations compost_recommendations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.compost_recommendations
    ADD CONSTRAINT compost_recommendations_pkey PRIMARY KEY (id);


--
-- Name: maintenance_schedules maintenance_schedules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.maintenance_schedules
    ADD CONSTRAINT maintenance_schedules_pkey PRIMARY KEY (id);


--
-- Name: user_badges user_badges_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_badges
    ADD CONSTRAINT user_badges_pkey PRIMARY KEY (user_id, badge_id);


--
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: waste_categories waste_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.waste_categories
    ADD CONSTRAINT waste_categories_pkey PRIMARY KEY (id);


--
-- Name: waste_items waste_items_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.waste_items
    ADD CONSTRAINT waste_items_pkey PRIMARY KEY (id);


--
-- Name: compost_batches compost_batches_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.compost_batches
    ADD CONSTRAINT compost_batches_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: compost_recommendations compost_recommendations_batch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.compost_recommendations
    ADD CONSTRAINT compost_recommendations_batch_id_fkey FOREIGN KEY (batch_id) REFERENCES public.compost_batches(id) ON DELETE CASCADE;


--
-- Name: maintenance_schedules maintenance_schedules_batch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.maintenance_schedules
    ADD CONSTRAINT maintenance_schedules_batch_id_fkey FOREIGN KEY (batch_id) REFERENCES public.compost_batches(id) ON DELETE CASCADE;


--
-- Name: user_badges user_badges_badge_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_badges
    ADD CONSTRAINT user_badges_badge_id_fkey FOREIGN KEY (badge_id) REFERENCES public.badges(id) ON DELETE CASCADE;


--
-- Name: user_badges user_badges_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_badges
    ADD CONSTRAINT user_badges_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: waste_items waste_items_batch_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.waste_items
    ADD CONSTRAINT waste_items_batch_id_fkey FOREIGN KEY (batch_id) REFERENCES public.compost_batches(id) ON DELETE CASCADE;


--
-- Name: waste_items waste_items_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.waste_items
    ADD CONSTRAINT waste_items_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.waste_categories(id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--

\unrestrict HRRoauPzSOHwBDdH71AGjq50pbxNzQea4uCVcqoOWIJ8rMifrsHQqrdwafUKTle

